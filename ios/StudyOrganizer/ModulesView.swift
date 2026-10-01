import SwiftUI

struct ModulesView: View {
    let session: AuthSession

    @EnvironmentObject private var sessionManager: SessionManager
    @State private var modules: [StudyModule] = []
    @State private var isLoading = false
    @State private var hasLoaded = false
    @State private var errorMessage: String?
    @State private var reloadID = UUID()

    var body: some View {
        NavigationStack {
            List {
                if isLoading {
                    HStack {
                        Spacer()
                        ProgressView("Lernmodule werden geladen …")
                        Spacer()
                    }
                }

                if let errorMessage {
                    Section {
                        Label(
                            errorMessage,
                            systemImage: "exclamationmark.circle"
                        )
                        .foregroundStyle(.red)

                        Button("Erneut versuchen") {
                            reloadID = UUID()
                        }
                        .disabled(isLoading)
                    }
                }

                if hasLoaded && modules.isEmpty {
                    Text("Du hast noch keine Lernmodule.")
                        .foregroundStyle(.secondary)
                }

                ForEach(modules) { module in
                    VStack(alignment: .leading, spacing: 8) {
                        Text(module.name)
                            .font(.headline)

                        if let code = nonEmpty(module.code) {
                            Text(code)
                                .font(.subheadline)
                                .foregroundStyle(.secondary)
                        }

                        if let description = nonEmpty(module.description) {
                            Text(description)
                                .font(.body)
                        }

                        if module.isExternalCourseLinked {
                            Label(
                                "Mit externem Kurs verknüpft",
                                systemImage: "link"
                            )
                            .font(.footnote)
                            .foregroundStyle(.secondary)
                        }
                    }
                    .padding(.vertical, 6)
                }
            }
            .navigationTitle("Meine Lernmodule")
            .toolbar {
                ToolbarItem(placement: .topBarTrailing) {
                    Button("Abmelden") {
                        sessionManager.signOut()
                    }
                }
            }
            .task(id: reloadID) {
                await loadModules()
            }
            .refreshable {
                await loadModules()
            }
        }
    }

    private func nonEmpty(_ value: String?) -> String? {
        guard let value,
              !value.trimmingCharacters(
                  in: .whitespacesAndNewlines
              ).isEmpty else {
            return nil
        }

        return value
    }

    @MainActor
    private func loadModules() async {
        guard !isLoading,
              sessionManager.session?.accessToken == session.accessToken else {
            return
        }

        sessionManager.checkExpiration()

        guard sessionManager.session?.accessToken == session.accessToken else {
            return
        }

        isLoading = true
        errorMessage = nil
        defer { isLoading = false }

        do {
            let result = try await ModuleService().getAll(
                accessToken: session.accessToken
            )

            try Task.checkCancellation()
            sessionManager.checkExpiration()

            // Antworten einer alten Sitzung nicht mehr übernehmen.
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

            if let serviceError = error as? ModuleServiceError,
               case .unauthorized = serviceError {
                modules = []
                sessionManager.invalidateSession(
                    accessToken: session.accessToken
                )
                return
            }

            // Bereits geladene Module bleiben bei Fehlern erhalten.
            errorMessage = error.localizedDescription
        }
    }
}
