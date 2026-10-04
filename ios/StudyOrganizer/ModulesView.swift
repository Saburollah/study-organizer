import SwiftUI

struct ModulesView: View {
    let session: AuthSession

    @EnvironmentObject private var sessionManager: SessionManager

    @State private var modules: [StudyModule] = []
    @State private var isLoading = false
    @State private var hasLoaded = false
    @State private var errorMessage: String?
    @State private var reloadID = UUID()

    @State private var showCreateModule = false
    @State private var moduleToEdit: StudyModule?

    var body: some View {
        NavigationStack {
            ScrollView {
                LazyVStack(spacing: 14) {

                    header

                    if isLoading && modules.isEmpty {
                        loadingView
                    }

                    if let errorMessage {
                        errorCard(errorMessage)
                    }

                    if hasLoaded
                        && modules.isEmpty
                        && !isLoading {
                        emptyState
                    }

                    ForEach(modules) { module in
                        moduleCard(module)
                    }
                }
                .frame(maxWidth: 600)
                .frame(maxWidth: .infinity)
                .padding(.horizontal, 18)
                .padding(.top, 18)
                .padding(.bottom, 40)
            }
            .background(
                Color(uiColor: .systemGroupedBackground)
                    .ignoresSafeArea()
            )
            .refreshable {
                await loadModules()
            }
            .toolbar {
                ToolbarItem(placement: .topBarTrailing) {
                    Menu {
                        Button(role: .destructive) {
                            sessionManager.signOut()
                        } label: {
                            Label(
                                "Abmelden",
                                systemImage:
                                    "rectangle.portrait.and.arrow.right"
                            )
                        }
                    } label: {
                        Image(systemName: "ellipsis.circle")
                    }
                }
            }

            // MARK: - Neues Lernmodul

            .sheet(isPresented: $showCreateModule) {
                CreateModuleView(
                    session: session
                ) { module in

                    guard sessionManager
                        .session?
                        .accessToken
                        == session.accessToken else {
                        return
                    }

                    modules.removeAll {
                        $0.id == module.id
                    }

                    modules.insert(
                        module,
                        at: 0
                    )

                    hasLoaded = true
                    errorMessage = nil
                }
                .environmentObject(sessionManager)
            }

            // MARK: - Lernmodul bearbeiten

            .sheet(item: $moduleToEdit) { module in
                EditModuleView(
                    module: module,
                    session: session
                ) { updatedModule in

                    guard sessionManager
                        .session?
                        .accessToken
                        == session.accessToken else {
                        return
                    }

                    if let index = modules.firstIndex(
                        where: {
                            $0.id == updatedModule.id
                        }
                    ) {
                        modules[index] = updatedModule
                    }

                    errorMessage = nil
                }
                .environmentObject(sessionManager)
            }

            .task(id: reloadID) {
                await loadModules()
            }
        }
    }

    // MARK: - Header

    private var header: some View {
        HStack(
            alignment: .center,
            spacing: 16
        ) {
            VStack(
                alignment: .leading,
                spacing: 5
            ) {
                Text("Meine Lernmodule")
                    .font(
                        .system(
                            size: 32,
                            weight: .bold
                        )
                    )

                Text("Deine Fächer auf einen Blick.")
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
            }

            Spacer(minLength: 8)

            Button {
                showCreateModule = true
            } label: {
                Image(systemName: "plus")
                    .font(
                        .system(
                            size: 16,
                            weight: .semibold
                        )
                    )
                    .foregroundStyle(.white)
                    .frame(
                        width: 40,
                        height: 40
                    )
                    .background(
                        Color.accentColor,
                        in: Circle()
                    )
            }
            .buttonStyle(.plain)
            .disabled(isLoading)
            .accessibilityLabel(
                "Lernmodul hinzufügen"
            )
        }
        .padding(.bottom, 6)
    }

    // MARK: - Modul Card

    private func moduleCard(
        _ module: StudyModule
    ) -> some View {

        HStack(spacing: 0) {

            // Farbiger Streifen

            RoundedRectangle(
                cornerRadius: 3,
                style: .continuous
            )
            .fill(
                color(for: module.color)
            )
            .frame(width: 6)
            .padding(.vertical, 10)

            VStack(
                alignment: .leading,
                spacing: 8
            ) {

                // MARK: Name + Optionen

                HStack(
                    alignment: .top,
                    spacing: 12
                ) {

                    VStack(
                        alignment: .leading,
                        spacing: 6
                    ) {

                        Text(module.name)
                            .font(
                                .system(
                                    size: 18,
                                    weight: .semibold
                                )
                            )
                            .foregroundStyle(.primary)

                        if let code = nonEmpty(
                            module.code
                        ) {
                            Text(code)
                                .font(
                                    .system(
                                        size: 13,
                                        weight: .semibold
                                    )
                                )
                                .foregroundStyle(
                                    color(
                                        for: module.color
                                    )
                                )
                                .padding(
                                    .horizontal,
                                    8
                                )
                                .padding(
                                    .vertical,
                                    4
                                )
                                .background(
                                    color(
                                        for: module.color
                                    )
                                    .opacity(0.10),
                                    in: Capsule()
                                )
                        }
                    }

                    Spacer()

                    Menu {
                        Button {
                            moduleToEdit = module
                        } label: {
                            Label(
                                "Bearbeiten",
                                systemImage: "pencil"
                            )
                        }
                    } label: {
                        Image(systemName: "ellipsis")
                            .font(
                                .system(
                                    size: 16,
                                    weight: .semibold
                                )
                            )
                            .foregroundStyle(
                                .primary.opacity(0.55)
                            )
                            .frame(
                                width: 32,
                                height: 32
                            )
                            .contentShape(
                                Rectangle()
                            )
                    }
                    .buttonStyle(.plain)
                    .disabled(isLoading)
                    .accessibilityLabel(
                        "\(module.name) Optionen"
                    )
                }

                // MARK: Beschreibung

                if let description = nonEmpty(
                    module.description
                ) {
                    Text(description)
                        .font(
                            .system(size: 15)
                        )
                        .foregroundStyle(.secondary)
                        .lineLimit(3)
                        .fixedSize(
                            horizontal: false,
                            vertical: true
                        )
                }

                // MARK: Externer Kurs

                if module.isExternalCourseLinked {
                    Label(
                        "Mit externem Kurs verknüpft",
                        systemImage: "link"
                    )
                    .font(
                        .system(
                            size: 12,
                            weight: .medium
                        )
                    )
                    .foregroundStyle(.secondary)
                    .padding(
                        .horizontal,
                        9
                    )
                    .padding(
                        .vertical,
                        5
                    )
                    .background(
                        Color.secondary.opacity(0.08),
                        in: Capsule()
                    )
                }
            }
            .padding(.leading, 14)
            .padding(.trailing, 13)
            .padding(.vertical, 13)
        }
        .background(
            Color(uiColor: .systemBackground),
            in: RoundedRectangle(
                cornerRadius: 18,
                style: .continuous
            )
        )
        .overlay {
            RoundedRectangle(
                cornerRadius: 18,
                style: .continuous
            )
            .stroke(
                Color.secondary.opacity(0.11),
                lineWidth: 1
            )
        }
        .shadow(
            color: Color.black.opacity(0.035),
            radius: 9,
            y: 3
        )
    }

    // MARK: - Loading

    private var loadingView: some View {
        VStack(spacing: 12) {
            ProgressView()

            Text("Lernmodule werden geladen …")
                .font(.subheadline)
                .foregroundStyle(.secondary)
        }
        .frame(maxWidth: .infinity)
        .padding(.vertical, 50)
    }

    // MARK: - Empty State

    private var emptyState: some View {
        VStack(spacing: 16) {

            Image(systemName: "books.vertical")
                .font(
                    .system(
                        size: 30,
                        weight: .medium
                    )
                )
                .foregroundStyle(.secondary)

            VStack(spacing: 5) {
                Text("Noch keine Lernmodule")
                    .font(.headline)

                Text(
                    "Erstelle dein erstes Lernmodul und organisiere deine Fächer."
                )
                .font(.subheadline)
                .foregroundStyle(.secondary)
                .multilineTextAlignment(.center)
            }

            Button {
                showCreateModule = true
            } label: {
                Label(
                    "Lernmodul erstellen",
                    systemImage: "plus"
                )
                .fontWeight(.semibold)
            }
            .buttonStyle(.borderedProminent)
        }
        .frame(maxWidth: .infinity)
        .padding(.vertical, 42)
        .padding(.horizontal, 24)
        .background(
            Color(uiColor: .systemBackground),
            in: RoundedRectangle(
                cornerRadius: 18,
                style: .continuous
            )
        )
        .overlay {
            RoundedRectangle(
                cornerRadius: 18,
                style: .continuous
            )
            .stroke(
                Color.secondary.opacity(0.11),
                lineWidth: 1
            )
        }
    }

    // MARK: - Fehler

    private func errorCard(
        _ message: String
    ) -> some View {

        VStack(
            alignment: .leading,
            spacing: 12
        ) {

            HStack(
                alignment: .top,
                spacing: 10
            ) {
                Image(
                    systemName:
                        "exclamationmark.circle.fill"
                )
                .foregroundStyle(.red)

                Text(message)
                    .font(.subheadline)
                    .foregroundStyle(.red)
            }

            Button {
                reloadID = UUID()
            } label: {
                Label(
                    "Erneut versuchen",
                    systemImage: "arrow.clockwise"
                )
                .font(.subheadline)
                .fontWeight(.semibold)
            }
            .disabled(isLoading)
        }
        .frame(
            maxWidth: .infinity,
            alignment: .leading
        )
        .padding(16)
        .background(
            Color.red.opacity(0.07),
            in: RoundedRectangle(
                cornerRadius: 14,
                style: .continuous
            )
        )
    }

    // MARK: - Backend-Farbe

    private func color(
        for hex: String?
    ) -> Color {

        guard let hex,
              let parsedColor = Color(hex: hex) else {
            return .accentColor
        }

        return parsedColor
    }

    // MARK: - String prüfen

    private func nonEmpty(
        _ value: String?
    ) -> String? {

        guard let value,
              !value
                .trimmingCharacters(
                    in: .whitespacesAndNewlines
                )
                .isEmpty else {
            return nil
        }

        return value
    }

    // MARK: - Module laden

    @MainActor
    private func loadModules() async {

        guard !isLoading,
              !showCreateModule,
              moduleToEdit == nil,
              sessionManager.session?.accessToken
                == session.accessToken else {
            return
        }

        sessionManager.checkExpiration()

        guard sessionManager.session?.accessToken
                == session.accessToken else {
            return
        }

        isLoading = true
        errorMessage = nil

        defer {
            isLoading = false
        }

        do {
            let result =
                try await ModuleService().getAll(
                    accessToken:
                        session.accessToken
                )

            try Task.checkCancellation()

            sessionManager.checkExpiration()

            // Eine verspätete Antwort einer alten
            // Sitzung darf nicht übernommen werden.
            guard sessionManager.session?.accessToken
                    == session.accessToken else {
                return
            }

            modules = result
            hasLoaded = true

        } catch is CancellationError {
            return

        } catch {

            guard !Task.isCancelled,
                  sessionManager.session?.accessToken
                    == session.accessToken else {
                return
            }

            if let serviceError =
                error as? ModuleServiceError,
               case .unauthorized = serviceError {

                modules = []

                sessionManager.invalidateSession(
                    accessToken:
                        session.accessToken
                )

                return
            }

            // Bei einem Refresh-Fehler bleiben bereits
            // geladene Module weiterhin sichtbar.
            errorMessage =
                error.localizedDescription
        }
    }
}

// MARK: - Hex-Farbe

private extension Color {

    init?(hex: String) {
        var hex = hex.trimmingCharacters(
            in: .whitespacesAndNewlines
        )

        if hex.hasPrefix("#") {
            hex.removeFirst()
        }

        guard hex.count == 6,
              let value = UInt64(
                hex,
                radix: 16
              ) else {
            return nil
        }

        let red =
            Double(
                (value >> 16) & 0xFF
            ) / 255

        let green =
            Double(
                (value >> 8) & 0xFF
            ) / 255

        let blue =
            Double(
                value & 0xFF
            ) / 255

        self.init(
            red: red,
            green: green,
            blue: blue
        )
    }
}
