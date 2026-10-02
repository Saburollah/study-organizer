import SwiftUI

struct CreateModuleView: View {
    let session: AuthSession
    let onCreated: (StudyModule) -> Void

    @EnvironmentObject private var sessionManager: SessionManager
    @Environment(\.dismiss) private var dismiss

    @State private var name = ""
    @State private var code = ""
    @State private var description = ""
    @State private var submitted = false
    @State private var isSubmitting = false
    @State private var requestError: String?

    private var normalizedName: String {
        name.trimmingCharacters(in: .whitespacesAndNewlines)
    }

    private var normalizedCode: String {
        code.trimmingCharacters(in: .whitespacesAndNewlines)
    }

    private var normalizedDescription: String {
        description.trimmingCharacters(in: .whitespacesAndNewlines)
    }

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

    var body: some View {
        NavigationStack {
            Form {
                Section {
                    TextField("Name", text: $name)
                        .accessibilityLabel("Name")

                    if submitted, let nameError {
                        errorLabel(nameError)
                    }
                } header: {
                    Text("Name · Pflichtfeld")
                } footer: {
                    Text("Maximal 100 Zeichen")
                }

                Section {
                    TextField("Zum Beispiel M3", text: $code)
                        .autocorrectionDisabled()
                        .textInputAutocapitalization(.never)
                        .accessibilityLabel("Code")

                    if submitted, let codeError {
                        errorLabel(codeError)
                    }
                } header: {
                    Text("Code · optional")
                } footer: {
                    Text("Maximal 30 Zeichen")
                }

                Section {
                    TextField(
                        "Beschreibung",
                        text: $description,
                        axis: .vertical
                    )
                    .lineLimit(4...10)
                    .accessibilityLabel("Beschreibung")

                    if submitted, let descriptionError {
                        errorLabel(descriptionError)
                    }
                } header: {
                    Text("Beschreibung · optional")
                } footer: {
                    Text("Maximal 1.000 Zeichen")
                }

                if isSubmitting {
                    Section {
                        ProgressView("Lernmodul wird gespeichert …")
                    }
                }

                if let requestError {
                    Section {
                        errorLabel(requestError)
                    }
                }
            }
            .disabled(isSubmitting)
            .scrollDismissesKeyboard(.interactively)
            .navigationTitle("Neues Lernmodul")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Abbrechen") {
                        dismiss()
                    }
                    .disabled(isSubmitting)
                }

                ToolbarItem(placement: .confirmationAction) {
                    Button("Speichern") {
                        save()
                    }
                    .disabled(isSubmitting)
                }
            }
            .interactiveDismissDisabled(isSubmitting)
        }
        .onChange(of: sessionManager.session?.accessToken) {
            if sessionManager.session?.accessToken != session.accessToken {
                dismiss()
            }
        }
    }

    private func errorLabel(_ message: String) -> some View {
        Label(message, systemImage: "exclamationmark.circle.fill")
            .font(.footnote)
            .foregroundStyle(.red)
            .fixedSize(horizontal: false, vertical: true)
    }

    @MainActor
    private func save() {
        guard !isSubmitting else { return }

        requestError = nil
        submitted = true

        guard nameError == nil,
              codeError == nil,
              descriptionError == nil else {
            return
        }

        sessionManager.checkExpiration()

        guard sessionManager.session?.accessToken == session.accessToken else {
            dismiss()
            return
        }

        let input = CreateModuleRequest(
            name: normalizedName,
            code: normalizedCode.isEmpty ? nil : normalizedCode,
            description: normalizedDescription.isEmpty
                ? nil
                : normalizedDescription
        )

        isSubmitting = true

        Task { @MainActor in
            defer { isSubmitting = false }

            do {
                let module = try await ModuleService().create(
                    input,
                    accessToken: session.accessToken
                )

                sessionManager.checkExpiration()

                guard sessionManager.session?.accessToken
                        == session.accessToken else {
                    return
                }

                onCreated(module)
                dismiss()
            } catch {
                sessionManager.checkExpiration()

                guard sessionManager.session?.accessToken
                        == session.accessToken else {
                    return
                }

                if let serviceError = error as? ModuleServiceError,
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
