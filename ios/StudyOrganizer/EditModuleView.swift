import SwiftUI

struct EditModuleView: View {
    let module: StudyModule
    let session: AuthSession
    let onUpdated: (StudyModule) -> Void

    @EnvironmentObject private var sessionManager: SessionManager
    @Environment(\.dismiss) private var dismiss

    @State private var name: String
    @State private var code: String
    @State private var description: String
    @State private var selectedColor: String

    @State private var submitted = false
    @State private var isSubmitting = false
    @State private var requestError: String?

    @FocusState private var focusedField: Field?

    private enum Field: Hashable {
        case name
        case code
        case description
    }

    // MARK: - Farben

    private let moduleColors: [(hex: String, color: Color)] = [
        (
            "#0C66E4",
            Color(
                red: 12 / 255,
                green: 102 / 255,
                blue: 228 / 255
            )
        ),
        (
            "#9333EA",
            Color(
                red: 147 / 255,
                green: 51 / 255,
                blue: 234 / 255
            )
        ),
        (
            "#16A34A",
            Color(
                red: 22 / 255,
                green: 163 / 255,
                blue: 74 / 255
            )
        ),
        (
            "#EA8A00",
            Color(
                red: 234 / 255,
                green: 138 / 255,
                blue: 0 / 255
            )
        ),
        (
            "#DC2626",
            Color(
                red: 220 / 255,
                green: 38 / 255,
                blue: 38 / 255
            )
        ),
        (
            "#DB2777",
            Color(
                red: 219 / 255,
                green: 39 / 255,
                blue: 119 / 255
            )
        ),
        (
            "#0891B2",
            Color(
                red: 8 / 255,
                green: 145 / 255,
                blue: 178 / 255
            )
        )
    ]

    // MARK: - Initialisierung

    init(
        module: StudyModule,
        session: AuthSession,
        onUpdated: @escaping (StudyModule) -> Void
    ) {
        self.module = module
        self.session = session
        self.onUpdated = onUpdated

        _name = State(initialValue: module.name)
        _code = State(initialValue: module.code ?? "")
        _description = State(
            initialValue: module.description ?? ""
        )

        _selectedColor = State(
            initialValue: module.color ?? "#0C66E4"
        )
    }

    // MARK: - Normalisierte Werte

    private var normalizedName: String {
        name.trimmingCharacters(
            in: .whitespacesAndNewlines
        )
    }

    private var normalizedCode: String {
        code.trimmingCharacters(
            in: .whitespacesAndNewlines
        )
    }

    private var normalizedDescription: String {
        description.trimmingCharacters(
            in: .whitespacesAndNewlines
        )
    }

    // MARK: - Validierung

    private var nameError: String? {
        if normalizedName.isEmpty {
            return "Bitte gib einen Namen ein."
        }

        if normalizedName.utf16.count > 100 {
            return "Der Name darf höchstens 100 Zeichen enthalten."
        }

        return nil
    }

    private var codeError: String? {
        normalizedCode.utf16.count > 30
            ? "Der Code darf höchstens 30 Zeichen enthalten."
            : nil
    }

    private var descriptionError: String? {
        normalizedDescription.utf16.count > 1000
            ? "Die Beschreibung darf höchstens 1.000 Zeichen enthalten."
            : nil
    }

    // MARK: - View

    var body: some View {
        NavigationStack {
            ScrollView {
                VStack(spacing: 20) {

                    header

                    formCard

                    if module.isExternalCourseLinked {
                        externalCourseCard
                    }

                    if let requestError {
                        serverErrorCard(requestError)
                    }

                    saveButton
                }
                .frame(maxWidth: 520)
                .frame(maxWidth: .infinity)
                .padding(.horizontal, 18)
                .padding(.top, 18)
                .padding(.bottom, 40)
            }
            .background(
                Color(uiColor: .systemGroupedBackground)
                    .ignoresSafeArea()
            )
            .contentShape(Rectangle())
            .onTapGesture {
                focusedField = nil
            }
            .scrollDismissesKeyboard(.interactively)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Abbrechen") {
                        dismiss()
                    }
                    .disabled(isSubmitting)
                }
            }
            .interactiveDismissDisabled(isSubmitting)
        }
        .onChange(of: sessionManager.session?.accessToken) {
            if sessionManager.session?.accessToken
                != session.accessToken {
                dismiss()
            }
        }
    }

    // MARK: - Header

    private var header: some View {
        VStack(spacing: 10) {
            Image(systemName: "square.and.pencil")
                .font(
                    .system(
                        size: 27,
                        weight: .semibold
                    )
                )
                .foregroundStyle(.tint)
                .frame(width: 60, height: 60)
                .background(
                    Color.accentColor.opacity(0.11),
                    in: RoundedRectangle(
                        cornerRadius: 17,
                        style: .continuous
                    )
                )
                .accessibilityHidden(true)

            Text("Lernmodul bearbeiten")
                .font(.title2)
                .fontWeight(.bold)

            Text("Passe dein Lernmodul an.")
                .font(.subheadline)
                .foregroundStyle(.secondary)
        }
        .frame(maxWidth: .infinity)
    }

    // MARK: - Formular

    private var formCard: some View {
        VStack(alignment: .leading, spacing: 18) {

            // Name

            VStack(alignment: .leading, spacing: 8) {
                HStack(spacing: 3) {
                    Text("Name")
                        .font(
                            .system(
                                size: 16,
                                weight: .semibold
                            )
                        )

                    Text("*")
                        .font(
                            .system(
                                size: 16,
                                weight: .semibold
                            )
                        )
                        .foregroundStyle(.red)
                }

                inputField(
                    icon: "book.closed",
                    placeholder: "z. B. Mathematik 2",
                    text: $name,
                    field: .name
                )
                .submitLabel(.next)
                .onSubmit {
                    focusedField = .code
                }

                if submitted, let nameError {
                    errorLabel(nameError)
                }
            }

            // Modulcode

            VStack(alignment: .leading, spacing: 8) {
                Text("Modulcode")
                    .font(
                        .system(
                            size: 16,
                            weight: .semibold
                        )
                    )

                inputField(
                    icon: "number",
                    placeholder: "z. B. M2",
                    text: $code,
                    field: .code
                )
                .textInputAutocapitalization(.characters)
                .autocorrectionDisabled()
                .submitLabel(.next)
                .onSubmit {
                    focusedField = .description
                }

                if submitted, let codeError {
                    errorLabel(codeError)
                }
            }

            // Beschreibung

            VStack(alignment: .leading, spacing: 8) {
                Text("Beschreibung")
                    .font(
                        .system(
                            size: 16,
                            weight: .semibold
                        )
                    )

                descriptionField

                if submitted, let descriptionError {
                    errorLabel(descriptionError)
                }
            }

            // Farbe

            VStack(alignment: .leading, spacing: 8) {
                Text("Farbe")
                    .font(
                        .system(
                            size: 16,
                            weight: .semibold
                        )
                    )

                colorSelection
            }
        }
        .padding(18)
        .background(
            Color(uiColor: .systemBackground),
            in: RoundedRectangle(
                cornerRadius: 22,
                style: .continuous
            )
        )
        .overlay {
            RoundedRectangle(
                cornerRadius: 22,
                style: .continuous
            )
            .stroke(
                Color.secondary.opacity(0.12),
                lineWidth: 1
            )
        }
        .shadow(
            color: Color.black.opacity(0.04),
            radius: 12,
            y: 4
        )
    }

    // MARK: - Eingabefeld

    private func inputField(
        icon: String,
        placeholder: String,
        text: Binding<String>,
        field: Field
    ) -> some View {
        HStack(spacing: 11) {
            Image(systemName: icon)
                .font(
                    .system(
                        size: 16,
                        weight: .medium
                    )
                )
                .foregroundStyle(
                    focusedField == field
                        ? Color.accentColor
                        : Color.secondary
                )
                .frame(width: 20)
                .accessibilityHidden(true)

            TextField(
                placeholder,
                text: text
            )
            .focused(
                $focusedField,
                equals: field
            )
        }
        .padding(.horizontal, 13)
        .frame(minHeight: 46)
        .background(
            Color(uiColor: .secondarySystemBackground),
            in: RoundedRectangle(
                cornerRadius: 12,
                style: .continuous
            )
        )
        .overlay {
            RoundedRectangle(
                cornerRadius: 12,
                style: .continuous
            )
            .stroke(
                borderColor(for: field),
                lineWidth:
                    focusedField == field
                        ? 1.7
                        : 1
            )
        }
        .animation(
            .easeOut(duration: 0.16),
            value: focusedField
        )
    }

    // MARK: - Beschreibung

    private var descriptionField: some View {
        HStack(alignment: .top, spacing: 11) {
            Image(systemName: "text.alignleft")
                .font(
                    .system(
                        size: 16,
                        weight: .medium
                    )
                )
                .foregroundStyle(
                    focusedField == .description
                        ? Color.accentColor
                        : Color.secondary
                )
                .frame(width: 20)
                .padding(.top, 2)
                .accessibilityHidden(true)

            TextField(
                "Worum geht es in diesem Lernmodul?",
                text: $description,
                axis: .vertical
            )
            .lineLimit(3...5)
            .focused(
                $focusedField,
                equals: .description
            )
            .accessibilityLabel("Beschreibung")
        }
        .padding(13)
        .frame(
            minHeight: 90,
            alignment: .top
        )
        .background(
            Color(uiColor: .secondarySystemBackground),
            in: RoundedRectangle(
                cornerRadius: 12,
                style: .continuous
            )
        )
        .overlay {
            RoundedRectangle(
                cornerRadius: 12,
                style: .continuous
            )
            .stroke(
                borderColor(for: .description),
                lineWidth:
                    focusedField == .description
                        ? 1.7
                        : 1
            )
        }
        .animation(
            .easeOut(duration: 0.16),
            value: focusedField
        )
    }

    // MARK: - Farbauswahl

    private var colorSelection: some View {
        HStack(spacing: 12) {
            ForEach(moduleColors, id: \.hex) { item in
                Button {
                    withAnimation(
                        .easeInOut(duration: 0.15)
                    ) {
                        selectedColor = item.hex
                    }
                } label: {
                    ZStack {
                        Circle()
                            .fill(item.color)
                            .frame(
                                width: 27,
                                height: 27
                            )

                        if selectedColor == item.hex {
                            Image(systemName: "checkmark")
                                .font(
                                    .system(
                                        size: 12,
                                        weight: .bold
                                    )
                                )
                                .foregroundStyle(.white)
                        }
                    }
                    .overlay {
                        if selectedColor == item.hex {
                            Circle()
                                .stroke(
                                    item.color.opacity(0.28),
                                    lineWidth: 2.5
                                )
                                .padding(-3)
                        }
                    }
                    .frame(
                        width: 34,
                        height: 34
                    )
                }
                .buttonStyle(.plain)
                .accessibilityLabel(
                    "Modulfarbe \(item.hex)"
                )
                .accessibilityAddTraits(
                    selectedColor == item.hex
                        ? .isSelected
                        : []
                )
            }
        }
        .frame(maxWidth: .infinity)
    }

    // MARK: - Externer Kurs

    private var externalCourseCard: some View {
        HStack(spacing: 10) {
            Image(systemName: "link")
                .foregroundStyle(.secondary)

            Text("Mit externem Kurs verknüpft")
                .font(.subheadline)
                .foregroundStyle(.secondary)

            Spacer()
        }
        .padding(14)
        .frame(maxWidth: .infinity)
        .background(
            Color(uiColor: .systemBackground),
            in: RoundedRectangle(
                cornerRadius: 14,
                style: .continuous
            )
        )
    }

    // MARK: - Speichern Button

    private var saveButton: some View {
        Button {
            focusedField = nil
            save()
        } label: {
            HStack(spacing: 8) {
                if isSubmitting {
                    ProgressView()
                        .tint(.white)
                }

                Text(
                    isSubmitting
                        ? "Wird gespeichert …"
                        : "Änderungen speichern"
                )
                .fontWeight(.semibold)
            }
            .foregroundStyle(.white)
            .frame(maxWidth: .infinity)
            .frame(height: 52)
            .background(
                Color.accentColor,
                in: RoundedRectangle(
                    cornerRadius: 14,
                    style: .continuous
                )
            )
        }
        .buttonStyle(.plain)
        .disabled(isSubmitting)
        .opacity(
            isSubmitting
                ? 0.75
                : 1
        )
    }

    // MARK: - Serverfehler

    private func serverErrorCard(
        _ message: String
    ) -> some View {
        HStack(alignment: .top, spacing: 10) {
            Image(
                systemName: "exclamationmark.circle.fill"
            )
            .foregroundStyle(.red)

            Text(message)
                .font(.subheadline)
                .foregroundStyle(.red)

            Spacer(minLength: 0)
        }
        .padding(14)
        .frame(maxWidth: .infinity)
        .background(
            Color.red.opacity(0.07),
            in: RoundedRectangle(
                cornerRadius: 12,
                style: .continuous
            )
        )
    }

    // MARK: - Rahmenfarbe

    private func borderColor(
        for field: Field
    ) -> Color {
        if submitted {
            switch field {
            case .name:
                if nameError != nil {
                    return .red
                }

            case .code:
                if codeError != nil {
                    return .red
                }

            case .description:
                if descriptionError != nil {
                    return .red
                }
            }
        }

        if focusedField == field {
            return .accentColor
        }

        return Color.secondary.opacity(0.22)
    }

    // MARK: - Fehlermeldung

    private func errorLabel(
        _ message: String
    ) -> some View {
        Label(
            message,
            systemImage: "exclamationmark.circle.fill"
        )
        .font(.footnote)
        .foregroundStyle(.red)
        .fixedSize(
            horizontal: false,
            vertical: true
        )
    }

    // MARK: - Speichern

    @MainActor
    private func save() {
        guard !isSubmitting else {
            return
        }

        requestError = nil
        submitted = true

        guard nameError == nil,
              codeError == nil,
              descriptionError == nil else {

            if nameError != nil {
                focusedField = .name
            } else if codeError != nil {
                focusedField = .code
            } else if descriptionError != nil {
                focusedField = .description
            }

            return
        }

        sessionManager.checkExpiration()

        guard sessionManager.session?.accessToken
                == session.accessToken else {
            dismiss()
            return
        }

        let input = UpdateModuleRequest(
            name: normalizedName,
            code: normalizedCode.isEmpty
                ? nil
                : normalizedCode,
            description: normalizedDescription.isEmpty
                ? nil
                : normalizedDescription,
            color: selectedColor
        )

        focusedField = nil
        isSubmitting = true

        Task { @MainActor in
            defer {
                isSubmitting = false
            }

            do {
                let updatedModule =
                    try await ModuleService().update(
                        moduleId: module.id,
                        input: input,
                        accessToken: session.accessToken
                    )

                sessionManager.checkExpiration()

                guard sessionManager.session?.accessToken
                        == session.accessToken else {
                    return
                }

                onUpdated(updatedModule)
                dismiss()

            } catch {
                sessionManager.checkExpiration()

                guard sessionManager.session?.accessToken
                        == session.accessToken else {
                    return
                }

                if let serviceError =
                    error as? ModuleServiceError,
                   case .unauthorized = serviceError {

                    sessionManager.invalidateSession(
                        accessToken: session.accessToken
                    )

                    dismiss()
                    return
                }

                requestError = error.localizedDescription
            }
        }
    }
}
