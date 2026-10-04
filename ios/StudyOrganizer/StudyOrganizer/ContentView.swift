import SwiftUI

struct ContentView: View {

    var body: some View {
        NavigationStack {
            ScrollView {
                VStack(spacing: 0) {

                    Spacer()
                        .frame(height: 44)

                    Image(systemName: "graduationcap.fill")
                        .font(.system(size: 38, weight: .semibold))
                        .foregroundStyle(.tint)
                        .frame(width: 84, height: 84)
                        .background(
                            Color.accentColor.opacity(0.11),
                            in: RoundedRectangle(
                                cornerRadius: 22,
                                style: .continuous
                            )
                        )
                        .accessibilityHidden(true)

                    Text("Study Organizer")
                        .font(.system(size: 19, weight: .semibold))
                        .foregroundStyle(.tint)
                        .padding(.top, 14)

                    VStack(spacing: 12) {
                        Text("Dein Studium.\nEinfach organisiert.")
                            .font(.system(size: 32, weight: .bold))
                            .multilineTextAlignment(.center)
                            .tracking(-0.5)

                        Text(
                            "Kurse, Aufgaben und Termine an einem Ort."
                        )
                        .font(.system(size: 17))
                        .foregroundStyle(.secondary)
                        .multilineTextAlignment(.center)
                    }
                    .padding(.top, 28)

                    Spacer()
                        .frame(height: 58)

                    NavigationLink {
                        LoginView()
                    } label: {
                        HStack(spacing: 10) {
                            Text("Anmelden")
                                .fontWeight(.semibold)

                            Image(systemName: "arrow.right")
                                .font(
                                    .system(
                                        size: 14,
                                        weight: .semibold
                                    )
                                )
                        }
                        .foregroundStyle(.white)
                        .frame(maxWidth: .infinity)
                        .frame(height: 54)
                        .background(
                            Color.accentColor,
                            in: RoundedRectangle(
                                cornerRadius: 14,
                                style: .continuous
                            )
                        )
                    }
                    .buttonStyle(.plain)

                    HStack(spacing: 5) {
                        Text("Noch kein Konto?")
                            .foregroundStyle(.secondary)

                        NavigationLink {
                            RegistrationView()
                        } label: {
                            Text("Registrieren")
                                .fontWeight(.semibold)
                        }
                    }
                    .font(.subheadline)
                    .padding(.top, 18)

                    Spacer()
                        .frame(height: 30)
                }
                .frame(maxWidth: 480)
                .frame(maxWidth: .infinity)
                .padding(.horizontal, 28)
            }
            .background(
                Color(uiColor: .systemGroupedBackground)
                    .ignoresSafeArea()
            )
        }
    }
}


// MARK: - Login

private struct LoginView: View {

    @EnvironmentObject private var sessionManager: SessionManager

    @State private var isSubmitting = false
    @State private var requestError: String?

    @State private var email = ""
    @State private var password = ""

    @State private var emailError: String?
    @State private var passwordError: String?

    @State private var isPasswordVisible = false

    @FocusState private var focusedField: Field?

    private enum Field: Hashable {
        case email
        case password
    }

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 24) {

                // MARK: Header

                VStack(spacing: 12) {
                    Image(systemName: "graduationcap.fill")
                        .font(.system(size: 28, weight: .semibold))
                        .foregroundStyle(.tint)
                        .frame(width: 64, height: 64)
                        .background(
                            Color.accentColor.opacity(0.12),
                            in: RoundedRectangle(
                                cornerRadius: 18,
                                style: .continuous
                            )
                        )
                        .accessibilityHidden(true)

                    Text("Willkommen zurück")
                        .font(.title.bold())

                    Text(
                        "Melde dich bei deinem Study Organizer Konto an."
                    )
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
                    .multilineTextAlignment(.center)
                }
                .frame(maxWidth: .infinity)

                // MARK: E-Mail

                VStack(alignment: .leading, spacing: 8) {
                    Text("E-Mail-Adresse")
                        .font(.subheadline)
                        .fontWeight(.semibold)

                    HStack(spacing: 12) {
                        Image(systemName: "envelope.fill")
                            .font(.system(size: 17, weight: .medium))
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
                            prompt: Text("E-Mail-Adresse")
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
                        .accessibilityLabel("E-Mail-Adresse")
                        .onSubmit {
                            focusedField = .password
                        }
                        .onChange(of: email) {
                            emailError = nil
                        }
                    }
                    .padding(.horizontal, 16)
                    .padding(.vertical, 12)
                    .frame(minHeight: 56)
                    .background {
                        RoundedRectangle(cornerRadius: 14)
                            .fill(
                                Color(uiColor: .systemBackground)
                            )
                    }
                    .overlay {
                        RoundedRectangle(cornerRadius: 14)
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

                    if let emailError {
                        errorMessage(emailError)
                    }
                }

                // MARK: Passwort

                VStack(alignment: .leading, spacing: 8) {
                    Text("Passwort")
                        .font(.subheadline)
                        .fontWeight(.semibold)

                    HStack(spacing: 12) {
                        Image(systemName: "lock.fill")
                            .font(.system(size: 17, weight: .medium))
                            .foregroundStyle(
                                focusedField == .password
                                    ? Color.accentColor
                                    : Color.secondary
                            )
                            .frame(width: 22)
                            .accessibilityHidden(true)

                        Group {
                            if isPasswordVisible {
                                TextField(
                                    "",
                                    text: $password,
                                    prompt: Text("Passwort")
                                        .foregroundStyle(
                                            Color.secondary.opacity(0.45)
                                        )
                                )
                            } else {
                                SecureField(
                                    "",
                                    text: $password,
                                    prompt: Text("Passwort")
                                        .foregroundStyle(
                                            Color.secondary.opacity(0.45)
                                        )
                                )
                            }
                        }
                        .textContentType(.password)
                        .textInputAutocapitalization(.never)
                        .autocorrectionDisabled()
                        .focused(
                            $focusedField,
                            equals: .password
                        )
                        .submitLabel(.go)
                        .accessibilityLabel("Passwort")
                        .onSubmit {
                            validate()
                        }
                        .onChange(of: password) {
                            passwordError = nil
                        }

                        Button {
                            isPasswordVisible.toggle()
                            focusedField = .password
                        } label: {
                            Image(
                                systemName:
                                    isPasswordVisible
                                        ? "eye.slash"
                                        : "eye"
                            )
                            .font(.system(size: 17, weight: .medium))
                            .foregroundStyle(.secondary)
                            .frame(width: 32, height: 32)
                        }
                        .buttonStyle(.plain)
                        .accessibilityLabel(
                            isPasswordVisible
                                ? "Passwort ausblenden"
                                : "Passwort anzeigen"
                        )
                    }
                    .padding(.horizontal, 16)
                    .padding(.vertical, 12)
                    .frame(minHeight: 56)
                    .background {
                        RoundedRectangle(cornerRadius: 14)
                            .fill(
                                Color(uiColor: .systemBackground)
                            )
                    }
                    .overlay {
                        RoundedRectangle(cornerRadius: 14)
                            .stroke(
                                passwordBorderColor,
                                lineWidth:
                                    focusedField == .password
                                        ? 2
                                        : 1
                            )
                    }
                    .shadow(
                        color:
                            focusedField == .password
                                ? Color.black.opacity(0.14)
                                : Color.black.opacity(0.05),
                        radius:
                            focusedField == .password
                                ? 10
                                : 4,
                        y:
                            focusedField == .password
                                ? 6
                                : 2
                    )
                    .offset(
                        y:
                            focusedField == .password
                                ? -2
                                : 0
                    )
                    .animation(
                        .easeOut(duration: 0.18),
                        value: focusedField
                    )

                    if let passwordError {
                        errorMessage(passwordError)
                    }
                }

                // MARK: Anmelden

                Button {
                    validate()
                } label: {
                    HStack(spacing: 8) {
                        Text(
                            isSubmitting
                                ? "Anmeldung läuft …"
                                : "Anmelden"
                        )
                        .fontWeight(.semibold)

                        Image(systemName: "arrow.right")
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
                    .background(Color.accentColor)
                    .clipShape(
                        RoundedRectangle(cornerRadius: 14)
                    )
                    .shadow(
                        color: Color.accentColor.opacity(0.22),
                        radius: 8,
                        y: 4
                    )
                }
                .buttonStyle(.plain)
                .padding(.top, 4)

                if isSubmitting {
                    ProgressView("Anmeldung läuft")
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
        .contentShape(Rectangle())
        .onTapGesture {
            focusedField = nil
        }
        .navigationTitle("Anmelden")
        .navigationBarTitleDisplayMode(.inline)
        .scrollDismissesKeyboard(.interactively)
        .disabled(isSubmitting)
        .navigationBarBackButtonHidden(isSubmitting)
    }

    private var emailBorderColor: Color {
        if emailError != nil {
            return .red
        }

        if focusedField == .email {
            return .accentColor
        }

        return Color.secondary.opacity(0.18)
    }

    private var passwordBorderColor: Color {
        if passwordError != nil {
            return .red
        }

        if focusedField == .password {
            return .accentColor
        }

        return Color.secondary.opacity(0.18)
    }

    private func errorMessage(
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

    @MainActor
    private func validate() {
        guard !isSubmitting else {
            return
        }

        requestError = nil

        email = email.trimmingCharacters(
            in: .whitespacesAndNewlines
        )

        if email.isEmpty {
            emailError =
                "Bitte gib deine E-Mail-Adresse ein."
        } else if !isPlausibleEmail(email) {
            emailError =
                "Bitte gib eine gültige E-Mail-Adresse ein."
        } else {
            emailError = nil
        }

        passwordError =
            password.isEmpty
                ? "Bitte gib dein Passwort ein."
                : nil

        if emailError != nil {
            focusedField = .email
            return
        }

        if passwordError != nil {
            focusedField = .password
            return
        }

        focusedField = nil
        isSubmitting = true

        let submittedEmail = email
        let submittedPassword = password

        Task { @MainActor in
            defer {
                isSubmitting = false
            }

            do {
                try await sessionManager.signIn(
                    email: submittedEmail,
                    password: submittedPassword
                )

                password = ""
                isPasswordVisible = false

            } catch is CancellationError {
                requestError =
                    "Die Anmeldung wurde abgebrochen."

            } catch {
                requestError =
                    error.localizedDescription
            }
        }
    }

    private func isPlausibleEmail(
        _ value: String
    ) -> Bool {

        let pattern =
            #"^[^\s@]+@[^\s@.]+(?:\.[^\s@.]+)+$"#

        return value.range(
            of: pattern,
            options: .regularExpression
        ) != nil
    }
}


#Preview {
    ContentView()
        .environmentObject(SessionManager())
}
