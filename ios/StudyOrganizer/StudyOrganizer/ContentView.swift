import SwiftUI

struct ContentView: View {
    var body: some View {
        NavigationStack {
            ScrollView {
                VStack(spacing: 16) {
                    Image(systemName: "graduationcap.fill")
                        .font(.system(size: 40, weight: .semibold))
                        .foregroundStyle(.tint)
                        .frame(width: 88, height: 88)
                        .background(.tint.opacity(0.12))
                        .clipShape(RoundedRectangle(cornerRadius: 22))
                        .accessibilityHidden(true)

                    VStack(spacing: 12) {
                        Text("Study Organizer")
                            .font(.title3)
                            .fontWeight(.semibold)
                            .foregroundStyle(.tint)

                        Text("Dein Studium. Einfach organisiert.")
                            .font(.largeTitle.bold())

                        Text("Kurse, Aufgaben und Termine an einem Ort.")
                            .font(.body)
                            .foregroundStyle(.secondary)
                    }
                    .multilineTextAlignment(.center)

                    VStack(spacing: 20) {
                        NavigationLink {
                            LoginView()
                        } label: {
                            HStack(spacing: 8) {
                                Text("Anmelden")
                                    .fontWeight(.semibold)

                                Image(systemName: "arrow.right")
                                    .font(.system(size: 14, weight: .semibold))
                            }
                            .frame(maxWidth: .infinity, minHeight: 48)
                        }
                        .buttonStyle(.borderedProminent)

                        VStack(spacing: 4) {
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
                    }
                }
                .frame(maxWidth: 480)
                .frame(maxWidth: .infinity)
                .padding(24)
                .padding(.top, 40)
            }
        }
    }
}

private struct AuthenticationPlaceholderView: View {
    let title: String
    let message: String

    var body: some View {
        ScrollView {
            VStack(spacing: 16) {
                Text(title)
                    .font(.title.bold())

                Text(message)
                    .foregroundStyle(.secondary)
            }
            .multilineTextAlignment(.center)
            .frame(maxWidth: .infinity)
            .padding(24)
        }
        .navigationTitle(title)
        .navigationBarTitleDisplayMode(.inline)
    }
}

private struct LoginView: View {

    // MARK: - Eingaben

    @State private var email = ""
    @State private var password = ""

    // MARK: - Fehlermeldungen

    @State private var emailError: String?
    @State private var passwordError: String?

    // MARK: - UI-Zustände

    @State private var showUnavailableAlert = false
    @State private var isPasswordVisible = false

    // Speichert, welches Eingabefeld gerade aktiv ist.
    @FocusState private var focusedField: Field?

    private enum Field: Hashable {
        case email
        case password
    }


    // MARK: - Benutzeroberfläche

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 24) {

                // MARK: Kopfbereich

                VStack(spacing: 16) {

                    Image(systemName: "graduationcap.fill")
                        .font(.system(size: 28, weight: .semibold))
                        .foregroundStyle(.tint)
                        .frame(width: 64, height: 64)
                        .background(
                            Color.accentColor.opacity(0.12)
                        )
                        .clipShape(
                            RoundedRectangle(cornerRadius: 18)
                        )

                    VStack(spacing: 6) {
                        Text("Willkommen zurück")
                            .font(.title.bold())

                        Text("Melde dich bei deinem Study Organizer Konto an.")
                            .font(.subheadline)
                            .foregroundStyle(.secondary)
                            .multilineTextAlignment(.center)
                    }
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

                        TextField(
                            "E-Mail-Adresse",
                            text: $email
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
                            .fill(Color(uiColor: .systemBackground))
                    }
                    .overlay {
                        RoundedRectangle(cornerRadius: 14)
                            .stroke(
                                emailBorderColor,
                                lineWidth: focusedField == .email ? 2 : 1
                            )
                    }
                    .shadow(
                        color: focusedField == .email
                            ? Color.black.opacity(0.14)
                            : Color.black.opacity(0.05),
                        radius: focusedField == .email ? 10 : 4,
                        y: focusedField == .email ? 6 : 2
                    )
                    .offset(y: focusedField == .email ? -2 : 0)
                    .animation(
                        .easeOut(duration: 0.18),
                        value: focusedField
                    )

                    if let emailError {
                        Label(
                            emailError,
                            systemImage:
                                "exclamationmark.circle.fill"
                        )
                        .font(.footnote)
                        .foregroundStyle(.red)
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

                        Group {
                            if isPasswordVisible {

                                TextField(
                                    "Passwort",
                                    text: $password
                                )

                            } else {

                                SecureField(
                                    "Passwort",
                                    text: $password
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


                        // Passwort anzeigen / verstecken

                        Button {
                            isPasswordVisible.toggle()
                            focusedField = .password
                        } label: {
                            Image(
                                systemName: isPasswordVisible
                                    ? "eye.slash"
                                    : "eye"
                            )
                            .font(.system(size: 17, weight: .medium))
                            .foregroundStyle(.secondary)
                            .frame(
                                width: 32,
                                height: 32
                            )
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
                            .fill(Color(uiColor: .systemBackground))
                    }
                    .overlay {
                        RoundedRectangle(cornerRadius: 14)
                            .stroke(
                                passwordBorderColor,
                                lineWidth: focusedField == .password ? 2 : 1
                            )
                    }
                    .shadow(
                        color: focusedField == .password
                            ? Color.black.opacity(0.14)
                            : Color.black.opacity(0.05),
                        radius: focusedField == .password ? 10 : 4,
                        y: focusedField == .password ? 6 : 2
                    )
                    .offset(y: focusedField == .password ? -2 : 0)
                    .animation(
                        .easeOut(duration: 0.18),
                        value: focusedField
                    )

                    if let passwordError {
                        Label(
                            passwordError,
                            systemImage:
                                "exclamationmark.circle.fill"
                        )
                        .font(.footnote)
                        .foregroundStyle(.red)
                    }
                }


                // MARK: Anmelden

                Button {
                    validate()
                } label: {
                    HStack(spacing: 8) {

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
                    .padding(.vertical, 12)
                    .frame(minHeight: 54)
                    .background(Color.accentColor)
                    .clipShape(
                        RoundedRectangle(cornerRadius: 14)
                    )
                    .shadow(
                        color:
                            Color.accentColor.opacity(0.22),
                        radius: 8,
                        y: 4
                    )
                }
                .buttonStyle(.plain)
                .padding(.top, 4)
            }
            .frame(maxWidth: 480)
            .frame(maxWidth: .infinity)
            .padding(24)
        }
        .background(
            Color.black.opacity(0.025)
                .ignoresSafeArea()
        )
        .contentShape(Rectangle())
        .onTapGesture {
            focusedField = nil
        }
        .navigationTitle("Anmelden")
        .navigationBarTitleDisplayMode(.inline)
        .scrollDismissesKeyboard(.interactively)

        // MARK: Alert

        .alert(
            "Die Anmeldung ist noch nicht verfügbar.",
            isPresented: $showUnavailableAlert
        ) {
            Button(
                "OK",
                role: .cancel
            ) { }
        }
    }


    // MARK: - Farben der Eingabefelder

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


    // MARK: - Schatten

    private var emailShadowColor: Color {

        if focusedField == .email {
            return Color.accentColor.opacity(0.12)
        }

        return Color.black.opacity(0.04)
    }


    private var passwordShadowColor: Color {

        if focusedField == .password {
            return Color.accentColor.opacity(0.12)
        }

        return Color.black.opacity(0.04)
    }


    // MARK: - Validierung

    private func validate() {

        let trimmedEmail = email.trimmingCharacters(
            in: .whitespacesAndNewlines
        )

        email = trimmedEmail


        // E-Mail prüfen

        if trimmedEmail.isEmpty {

            emailError =
                "Bitte gib deine E-Mail-Adresse ein."

        } else if !isPlausibleEmail(trimmedEmail) {

            emailError =
                "Bitte gib eine gültige E-Mail-Adresse ein."

        } else {

            emailError = nil
        }


        // Passwort prüfen

        passwordError =
            password.isEmpty
                ? "Bitte gib dein Passwort ein."
                : nil


        // Zum ersten fehlerhaften Feld springen

        if emailError != nil {

            focusedField = .email

        } else if passwordError != nil {

            focusedField = .password

        } else {

            focusedField = nil
            showUnavailableAlert = true
        }
    }


    // MARK: - E-Mail-Prüfung

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
}
