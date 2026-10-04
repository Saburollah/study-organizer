import SwiftUI

struct RegistrationView: View {

    @State private var email = ""
    @State private var password = ""
    @State private var confirmation = ""

    @State private var submitted = false
    @State private var isSubmitting = false
    @State private var showSuccessAlert = false
    @State private var registeredEmail = ""
    @State private var requestError: String?

    @FocusState private var focusedField: Field?

    private enum Field: Hashable {
        case email
    }

    private enum PasswordStrength {
        case weak
        case medium
        case strong
    }

    private var requirements: [(title: String, met: Bool)] {
        [
            (
                "15+",
                password.utf16.count >= 15
            ),
            (
                "A–Z",
                matches("[A-ZÄÖÜ]")
            ),
            (
                "a–z",
                matches("[a-zäöüß]")
            ),
            (
                "0–9",
                matches("[0-9]")
            ),
            (
                "@#$",
                password.contains { character in
                    "@#$%&*-_!+=:,.?/\\\"'();"
                        .contains(character)
                }
            )
        ]
    }

    private var fulfilledCount: Int {
        requirements.filter {
            $0.met
        }.count
    }

    private var passwordStrength: PasswordStrength {
        if fulfilledCount == 5 {
            return .strong
        }

        if fulfilledCount >= 3 {
            return .medium
        }

        return .weak
    }

    private var passwordStrengthColor: Color {
        switch passwordStrength {
        case .weak:
            return .red

        case .medium:
            return .orange

        case .strong:
            return .green
        }
    }

    private var emailError: String? {
        let normalized =
            email.trimmingCharacters(
                in: .whitespacesAndNewlines
            )

        if normalized.isEmpty {
            return "Bitte gib deine E-Mail-Adresse ein."
        }

        let pattern =
            #"^[^\s@]+@[^\s@.]+(?:\.[^\s@.]+)+$"#

        if normalized.range(
            of: pattern,
            options: .regularExpression
        ) == nil {
            return "Bitte gib eine gültige E-Mail-Adresse ein."
        }

        return nil
    }

    private var confirmationError: String? {
        if confirmation.isEmpty {
            return "Bitte bestätige dein Passwort."
        }

        if confirmation != password {
            return "Die Passwörter stimmen nicht überein."
        }

        return nil
    }

    var body: some View {
        ScrollView {
            VStack(
                alignment: .leading,
                spacing: 24
            ) {

                header

                // MARK: E-Mail

                VStack(
                    alignment: .leading,
                    spacing: 8
                ) {
                    Text("E-Mail-Adresse")
                        .font(.subheadline)
                        .fontWeight(.semibold)

                    HStack(spacing: 12) {
                        Image(systemName: "envelope.fill")
                            .font(
                                .system(
                                    size: 17,
                                    weight: .medium
                                )
                            )
                            .foregroundStyle(
                                focusedField == .email
                                    ? Color.accentColor
                                    : Color.secondary
                            )
                            .frame(width: 22)
                            .accessibilityHidden(true)

                        TextField(
                            "",
                            text: $email,
                            prompt:
                                Text("E-Mail-Adresse")
                                .foregroundStyle(
                                    Color.secondary.opacity(0.45)
                                )
                        )
                        .keyboardType(.emailAddress)
                        .textContentType(.emailAddress)
                        .textInputAutocapitalization(.never)
                        .autocorrectionDisabled()
                        .focused(
                            $focusedField,
                            equals: .email
                        )
                        .submitLabel(.next)
                        .accessibilityLabel(
                            "E-Mail-Adresse"
                        )
                    }
                    .padding(.horizontal, 16)
                    .padding(.vertical, 12)
                    .frame(minHeight: 56)
                    .background {
                        RoundedRectangle(
                            cornerRadius: 14
                        )
                        .fill(
                            Color(
                                uiColor: .systemBackground
                            )
                        )
                    }
                    .overlay {
                        RoundedRectangle(
                            cornerRadius: 14
                        )
                        .stroke(
                            emailBorderColor,
                            lineWidth:
                                focusedField == .email
                                    ? 2
                                    : 1
                        )
                    }
                    .shadow(
                        color:
                            focusedField == .email
                                ? Color.black.opacity(0.14)
                                : Color.black.opacity(0.05),
                        radius:
                            focusedField == .email
                                ? 10
                                : 4,
                        y:
                            focusedField == .email
                                ? 6
                                : 2
                    )
                    .offset(
                        y:
                            focusedField == .email
                                ? -2
                                : 0
                    )
                    .animation(
                        .easeOut(duration: 0.18),
                        value: focusedField
                    )

                    if submitted,
                       let emailError {
                        errorMessage(emailError)
                    }
                }

                // MARK: Passwort

                VStack(
                    alignment: .leading,
                    spacing: 12
                ) {
                    UnifiedRegistrationPasswordField(
                        title: "Passwort",
                        placeholder: "Passwort",
                        value: $password,
                        borderColor:
                            password.isEmpty
                                ? nil
                                : passwordStrengthColor
                    )

                    passwordStrengthView

                    if submitted &&
                        fulfilledCount < requirements.count {

                        errorMessage(
                            "Bitte erfülle alle fünf Passwortanforderungen."
                        )
                    }
                }

                // MARK: Bestätigung

                VStack(
                    alignment: .leading,
                    spacing: 8
                ) {
                    UnifiedRegistrationPasswordField(
                        title: "Passwort bestätigen",
                        placeholder: "Passwort bestätigen",
                        value: $confirmation,
                        borderColor: nil
                    )

                    if submitted,
                       let confirmationError {
                        errorMessage(
                            confirmationError
                        )
                    }
                }

                // MARK: Registrieren

                Button {
                    dismissKeyboard()
                    validate()
                } label: {
                    HStack(spacing: 8) {
                        Text(
                            isSubmitting
                                ? "Konto wird erstellt …"
                                : "Konto erstellen"
                        )
                        .fontWeight(.semibold)

                        Image(
                            systemName: "arrow.right"
                        )
                        .font(
                            .system(
                                size: 14,
                                weight: .semibold
                            )
                        )
                    }
                    .foregroundStyle(.white)
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 12)
                    .frame(minHeight: 54)
                    .background(
                        Color.accentColor
                    )
                    .clipShape(
                        RoundedRectangle(
                            cornerRadius: 14
                        )
                    )
                    .shadow(
                        color:
                            Color.accentColor.opacity(0.22),
                        radius: 8,
                        y: 4
                    )
                }
                .buttonStyle(.plain)

                if isSubmitting {
                    ProgressView(
                        "Registrierung läuft"
                    )
                    .frame(maxWidth: .infinity)
                }

                if let requestError {
                    errorMessage(requestError)
                }
            }
            .frame(maxWidth: 480)
            .frame(maxWidth: .infinity)
            .padding(24)
        }
        .background(
            Color(uiColor: .systemGroupedBackground)
                .ignoresSafeArea()
        )
        .disabled(isSubmitting)
        .navigationBarBackButtonHidden(
            isSubmitting
        )
        .contentShape(Rectangle())
        .onTapGesture {
            dismissKeyboard()
        }
        .navigationTitle(
            "Registrieren"
        )
        .navigationBarTitleDisplayMode(
            .inline
        )
        .scrollDismissesKeyboard(
            .interactively
        )
        .alert(
            "Konto erstellt",
            isPresented: $showSuccessAlert
        ) {
            Button(
                "OK",
                role: .cancel
            ) { }
        } message: {
            Text(
                "Das Konto für \(registeredEmail) wurde erfolgreich erstellt."
            )
        }
    }

    // MARK: Header

    private var header: some View {
        VStack(spacing: 12) {
            Image(
                systemName:
                    "graduationcap.fill"
            )
            .font(
                .system(
                    size: 28,
                    weight: .semibold
                )
            )
            .foregroundStyle(.tint)
            .frame(
                width: 64,
                height: 64
            )
            .background(
                Color.accentColor.opacity(0.12),
                in: RoundedRectangle(
                    cornerRadius: 18,
                    style: .continuous
                )
            )
            .accessibilityHidden(true)

            Text("Neues Konto")
                .font(.title.bold())

            Text(
                "Organisiere dein Studium an einem Ort."
            )
            .font(.subheadline)
            .foregroundStyle(.secondary)
        }
        .multilineTextAlignment(.center)
        .frame(maxWidth: .infinity)
    }

    // MARK: Passwortanforderungen

    private var passwordStrengthView: some View {
        VStack(
            alignment: .leading,
            spacing: 14
        ) {
            HStack {
                Text("Anforderungen")
                    .font(
                        .system(
                            size: 17,
                            weight: .medium
                        )
                    )

                Spacer()

                Text(
                    "\(fulfilledCount) von 5 erfüllt"
                )
                .font(
                    .system(
                        size: 16,
                        weight: .semibold
                    )
                )
                .foregroundStyle(
                    passwordStrengthColor
                )
            }

            GeometryReader { geometry in
                ZStack(
                    alignment: .leading
                ) {
                    Capsule()
                        .fill(
                            Color.secondary.opacity(
                                0.12
                            )
                        )

                    Capsule()
                        .fill(
                            passwordStrengthColor
                        )
                        .frame(
                            width:
                                geometry.size.width
                                * (
                                    Double(
                                        fulfilledCount
                                    )
                                    / Double(
                                        requirements.count
                                    )
                                )
                        )
                        .animation(
                            .easeInOut(
                                duration: 0.25
                            ),
                            value:
                                fulfilledCount
                        )
                }
            }
            .frame(height: 5)

            HStack(spacing: 0) {
                ForEach(
                    requirements,
                    id: \.title
                ) { requirement in

                    HStack(spacing: 6) {
                        Image(
                            systemName:
                                requirement.met
                                    ? "checkmark.circle.fill"
                                    : "circle.fill"
                        )
                        .font(
                            .system(size: 16)
                        )
                        .foregroundStyle(
                            requirement.met
                                ? Color.green
                                : Color.secondary.opacity(
                                    0.22
                                )
                        )

                        Text(
                            requirement.title
                        )
                        .font(
                            .system(
                                size: 15
                            )
                        )
                        .foregroundStyle(
                            requirement.met
                                ? Color.green
                                : Color.secondary
                        )
                        .lineLimit(1)
                    }

                    if requirement.title
                        != requirements.last?.title {
                        Spacer(
                            minLength: 5
                        )
                    }
                }
            }

            Text(
                "Sonderzeichen: @ # $ % & * - _ ! + = : , . ? / \\ \" ' ( ) ;"
            )
            .font(
                .system(size: 13)
            )
            .foregroundStyle(
                Color.secondary.opacity(
                    0.85
                )
            )
            .fixedSize(
                horizontal: false,
                vertical: true
            )
        }
    }

    private var emailBorderColor: Color {
        if submitted &&
            emailError != nil {
            return .red
        }

        if focusedField == .email {
            return .accentColor
        }

        return Color.secondary.opacity(
            0.18
        )
    }

    private func errorMessage(
        _ message: String
    ) -> some View {
        Label(
            message,
            systemImage:
                "exclamationmark.circle.fill"
        )
        .font(.footnote)
        .foregroundStyle(.red)
        .fixedSize(
            horizontal: false,
            vertical: true
        )
    }

    private func matches(
        _ pattern: String
    ) -> Bool {
        password.range(
            of: pattern,
            options: .regularExpression
        ) != nil
    }

    private func dismissKeyboard() {
        focusedField = nil

        UIApplication.shared.sendAction(
            #selector(
                UIResponder.resignFirstResponder
            ),
            to: nil,
            from: nil,
            for: nil
        )
    }

    @MainActor
    private func validate() {
        guard !isSubmitting else {
            return
        }

        requestError = nil

        email =
            email.trimmingCharacters(
                in: .whitespacesAndNewlines
            )

        submitted = true

        guard emailError == nil,
              fulfilledCount
                == requirements.count,
              confirmationError == nil
        else {
            return
        }

        dismissKeyboard()

        isSubmitting = true

        let submittedEmail = email
        let submittedPassword =
            password

        Task { @MainActor in
            defer {
                isSubmitting = false
            }

            do {
                let result =
                    try await RegistrationService()
                        .register(
                            email:
                                submittedEmail,
                            password:
                                submittedPassword
                        )

                registeredEmail =
                    result.email

                email = ""
                password = ""
                confirmation = ""

                submitted = false
                showSuccessAlert = true

            } catch is CancellationError {
                requestError =
                    "Die Anfrage wurde abgebrochen. "
                    + "Das Konto wurde möglicherweise bereits erstellt."

            } catch {
                requestError =
                    error.localizedDescription
            }
        }
    }
}


// MARK: - Einheitliches Passwortfeld

private struct UnifiedRegistrationPasswordField: View {

    let title: String
    let placeholder: String

    @Binding var value: String

    let borderColor: Color?

    @State private var isVisible = false

    @FocusState private var isFocused: Bool

    var body: some View {
        VStack(
            alignment: .leading,
            spacing: 8
        ) {

            Text(title)
                .font(.subheadline)
                .fontWeight(.semibold)

            HStack(spacing: 12) {
                Image(
                    systemName: "lock.fill"
                )
                .font(
                    .system(
                        size: 17,
                        weight: .medium
                    )
                )
                .foregroundStyle(
                    isFocused
                        ? Color.accentColor
                        : Color.secondary
                )
                .frame(width: 22)
                .accessibilityHidden(true)

                Group {
                    if isVisible {
                        TextField(
                            "",
                            text: $value,
                            prompt:
                                Text(
                                    placeholder
                                )
                                .foregroundStyle(
                                    Color.secondary.opacity(
                                        0.45
                                    )
                                )
                        )
                    } else {
                        SecureField(
                            "",
                            text: $value,
                            prompt:
                                Text(
                                    placeholder
                                )
                                .foregroundStyle(
                                    Color.secondary.opacity(
                                        0.45
                                    )
                                )
                        )
                    }
                }
                .textContentType(
                    .newPassword
                )
                .textInputAutocapitalization(
                    .never
                )
                .autocorrectionDisabled()
                .focused($isFocused)
                .accessibilityLabel(
                    title
                )

                Button {
                    isVisible.toggle()
                    isFocused = true
                } label: {
                    Image(
                        systemName:
                            isVisible
                                ? "eye.slash"
                                : "eye"
                    )
                    .font(
                        .system(
                            size: 17,
                            weight: .medium
                        )
                    )
                    .foregroundStyle(
                        .secondary
                    )
                    .frame(
                        width: 32,
                        height: 32
                    )
                }
                .buttonStyle(.plain)
                .accessibilityLabel(
                    "\(title) "
                    + (
                        isVisible
                            ? "ausblenden"
                            : "anzeigen"
                    )
                )
            }
            .padding(
                .horizontal,
                16
            )
            .padding(
                .vertical,
                12
            )
            .frame(
                minHeight: 56
            )
            .background {
                RoundedRectangle(
                    cornerRadius: 14
                )
                .fill(
                    Color(
                        uiColor:
                            .systemBackground
                    )
                )
            }
            .overlay {
                RoundedRectangle(
                    cornerRadius: 14
                )
                .stroke(
                    currentBorderColor,
                    lineWidth:
                        isFocused
                            ? 2
                            : 1
                )
            }
            .shadow(
                color:
                    isFocused
                        ? Color.black.opacity(
                            0.14
                        )
                        : Color.black.opacity(
                            0.05
                        ),
                radius:
                    isFocused
                        ? 10
                        : 4,
                y:
                    isFocused
                        ? 6
                        : 2
            )
            .offset(
                y:
                    isFocused
                        ? -2
                        : 0
            )
            .animation(
                .easeOut(
                    duration: 0.18
                ),
                value: isFocused
            )
        }
    }

    private var currentBorderColor: Color {
        if isFocused {
            return .accentColor
        }

        if let borderColor,
           !value.isEmpty {
            return borderColor
        }

        return Color.secondary.opacity(
            0.18
        )
    }
}


#Preview {
    NavigationStack {
        RegistrationView()
    }
}
