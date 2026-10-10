import SwiftUI

struct ChangePasswordView: View {

    let session: AuthSession

    @Environment(\.dismiss)
    private var dismiss

    @EnvironmentObject
    private var sessionManager: SessionManager


    @State
    private var currentPassword = ""

    @State
    private var newPassword = ""

    @State
    private var confirmation = ""

    @State
    private var submitted = false

    @State
    private var isSubmitting = false

    @State
    private var requestError: String?

    @State
    private var showSuccessAlert = false


    private enum PasswordStrength {

        case weak
        case medium
        case strong
    }


    private var requirements: [(title: String, met: Bool)] {

        [
            (
                "15+",
                newPassword.utf16.count >= 15
            ),

            (
                "A–Z",
                matches(
                    "[A-ZÄÖÜ]"
                )
            ),

            (
                "a–z",
                matches(
                    "[a-zäöüß]"
                )
            ),

            (
                "0–9",
                matches(
                    "[0-9]"
                )
            ),

            (
                "@#$",
                newPassword.contains { character in

                    "@#$%&*-_!+=:,.?/\\\"'();"
                        .contains(
                            character
                        )
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


    private var confirmationError: String? {

        if confirmation.isEmpty {

            return "Bitte bestätige dein neues Passwort."
        }


        if confirmation != newPassword {

            return "Die Passwörter stimmen nicht überein."
        }


        return nil
    }


    var body: some View {

        ScrollView {

            VStack(
                alignment: .leading,
                spacing: 22
            ) {

                header


                currentPasswordSection
                
                if submitted
                    && currentPassword.isEmpty {

                    errorMessage(
                        "Bitte gib dein aktuelles Passwort ein."
                    )
                }


                newPasswordSection


                confirmationSection


                if let requestError {

                    errorMessage(
                        requestError
                    )
                }


                changePasswordButton
            }
            .frame(
                maxWidth: 500
            )
            .frame(
                maxWidth: .infinity
            )
            .padding(
                .horizontal,
                20
            )
            .padding(
                .top,
                12
            )
            .padding(
                .bottom,
                40
            )
        }
        .background {

            LinearGradient(
                colors: [
                    Color(
                        uiColor:
                            .systemGroupedBackground
                    ),
                    Color.accentColor.opacity(
                        0.025
                    ),
                    Color(
                        uiColor:
                            .systemGroupedBackground
                    )
                ],
                startPoint:
                    .top,
                endPoint:
                    .bottom
            )
            .ignoresSafeArea()
        }
        .navigationTitle("")
        .navigationBarTitleDisplayMode(
            .inline
        )
        .toolbar {

            ToolbarItem(
                placement:
                    .topBarLeading
            ) {

                Button(
                    "Abbrechen"
                ) {

                    dismiss()
                }
                .font(
                    .system(
                        size: 15,
                        weight: .medium
                    )
                )
                .disabled(
                    isSubmitting
                )
            }


            ToolbarItem(
                placement:
                    .principal
            ) {

                Text(
                    "Passwort ändern"
                )
                .font(
                    .system(
                        size: 17,
                        weight: .semibold
                    )
                )
            }
        }
        .alert(
            "Passwort geändert",
            isPresented:
                $showSuccessAlert
        ) {

            Button(
                "OK",
                role:
                    .cancel
            ) {

                dismiss()
            }

        } message: {

            Text(
                "Dein Passwort wurde erfolgreich geändert."
            )
        }
    }


    // MARK: - Header

    private var header: some View {

        VStack(
            spacing: 10
        ) {

            ZStack {

                RoundedRectangle(
                    cornerRadius: 18,
                    style: .continuous
                )
                .fill(
                    LinearGradient(
                        colors: [
                            Color.accentColor.opacity(
                                0.18
                            ),
                            Color.accentColor.opacity(
                                0.08
                            )
                        ],
                        startPoint:
                            .topLeading,
                        endPoint:
                            .bottomTrailing
                    )
                )
                .frame(
                    width: 54,
                    height: 54
                )


                RoundedRectangle(
                    cornerRadius: 18,
                    style: .continuous
                )
                .stroke(
                    Color.white.opacity(
                        0.45
                    ),
                    lineWidth: 0.8
                )
                .frame(
                    width: 54,
                    height: 54
                )


                Image(
                    systemName:
                        "lock.fill"
                )
                .font(
                    .system(
                        size: 23,
                        weight: .semibold
                    )
                )
                .foregroundStyle(
                    Color.accentColor
                )
            }


            Text(
                "Passwort ändern"
            )
            .font(
                .system(
                    size: 22,
                    weight: .bold
                )
            )


            Text(
                "Verwende ein neues, einzigartiges Passwort für dein Konto."
            )
            .font(
                .system(
                    size: 14
                )
            )
            .foregroundStyle(
                .secondary
            )
            .multilineTextAlignment(
                .center
            )
        }
        .frame(
            maxWidth: .infinity
        )
    }


    // MARK: - Current Password

    private var currentPasswordSection: some View {

        UnifiedChangePasswordField(
            title:
                "Aktuelles Passwort",
            placeholder:
                "Aktuelles Passwort",
            value:
                $currentPassword,
            borderColor:
                nil,
            textContentType:
                .password
        )
    }


    // MARK: - New Password

    private var newPasswordSection: some View {

        VStack(
            alignment: .leading,
            spacing: 12
        ) {

            UnifiedChangePasswordField(
                title:
                    "Neues Passwort",
                placeholder:
                    "Neues Passwort",
                value:
                    $newPassword,
                borderColor:
                    newPassword.isEmpty
                        ? nil
                        : passwordStrengthColor,
                textContentType:
                    .newPassword
            )


            passwordStrengthView
        }
    }


    // MARK: - Confirmation

    private var confirmationSection: some View {

        VStack(
            alignment: .leading,
            spacing: 8
        ) {

            UnifiedChangePasswordField(
                title:
                    "Passwort bestätigen",
                placeholder:
                    "Passwort bestätigen",
                value:
                    $confirmation,
                borderColor:
                    nil,
                textContentType:
                    .newPassword
            )


            if submitted,
               let confirmationError {

                errorMessage(
                    confirmationError
                )
            }
        }
    }


    // MARK: - Requirements

    private var passwordStrengthView: some View {

        VStack(
            alignment: .leading,
            spacing: 14
        ) {

            HStack {

                Text(
                    "Anforderungen"
                )
                .font(
                    .system(
                        size: 15,
                        weight: .medium
                    )
                )


                Spacer()


                Text(
                    "\(fulfilledCount) von 5 erfüllt"
                )
                .font(
                    .system(
                        size: 14,
                        weight: .semibold
                    )
                )
                .foregroundStyle(
                    passwordStrengthColor
                )
            }


            GeometryReader { geometry in

                ZStack(
                    alignment:
                        .leading
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
            .frame(
                height: 5
            )


            HStack(
                spacing: 0
            ) {

                ForEach(
                    requirements,
                    id:
                        \.title
                ) { requirement in

                    HStack(
                        spacing: 6
                    ) {

                        Image(
                            systemName:
                                requirement.met
                                    ? "checkmark.circle.fill"
                                    : "circle.fill"
                        )
                        .font(
                            .system(
                                size: 16
                            )
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
                        .lineLimit(
                            1
                        )
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
                .system(
                    size: 13
                )
            )
            .foregroundStyle(
                Color.secondary.opacity(
                    0.85
                )
            )
        }
    }


    // MARK: - Button

    private var changePasswordButton: some View {

        Button {

            validate()

        } label: {

            HStack(
                spacing: 8
            ) {

                Text(
                    isSubmitting
                        ? "Passwort wird geändert …"
                        : "Passwort ändern"
                )


                if !isSubmitting {

                    Image(
                        systemName:
                            "arrow.right"
                    )
                }
            }
            .font(
                .system(
                    size: 16,
                    weight: .semibold
                )
            )
            .foregroundStyle(
                .white
            )
            .frame(
                maxWidth: .infinity
            )
            .frame(
                height: 54
            )
            .background(
                Color.accentColor,
                in:
                    RoundedRectangle(
                        cornerRadius: 14,
                        style: .continuous
                    )
            )
            .shadow(
                color:
                    Color.accentColor.opacity(
                        0.20
                    ),
                radius: 8,
                y: 4
            )
        }
        .buttonStyle(
            .plain
        )
    }


    // MARK: - Error

    private func errorMessage(
        _ message: String
    ) -> some View {

        Label(
            message,
            systemImage:
                "exclamationmark.circle.fill"
        )
        .font(
            .footnote
        )
        .foregroundStyle(
            .red
        )
    }


    // MARK: - Validation

    private func matches(
        _ pattern: String
    ) -> Bool {

        newPassword.range(
            of:
                pattern,
            options:
                .regularExpression
        ) != nil
    }


    @MainActor
    private func validate() {

        guard !isSubmitting else {

            return
        }


        requestError =
            nil

        submitted =
            true


        guard
            !currentPassword.isEmpty,
            fulfilledCount
                == requirements.count,
            confirmationError
                == nil

        else {

            return
        }


        let submittedCurrentPassword =
            currentPassword

        let submittedNewPassword =
            newPassword


        isSubmitting =
            true


        Task { @MainActor in

            defer {

                isSubmitting =
                    false
            }


            do {

                try await PasswordService()
                    .changePassword(
                        accessToken:
                            session.accessToken,
                        currentPassword:
                            submittedCurrentPassword,
                        newPassword:
                            submittedNewPassword
                    )


                // Nur nach erfolgreicher Antwort
                // des Backends zurücksetzen.
                currentPassword =
                    ""

                newPassword =
                    ""

                confirmation =
                    ""

                submitted =
                    false

                requestError =
                    nil

                showSuccessAlert =
                    true


            } catch PasswordServiceError.unauthorized {

                // Eine abgelaufene/ungültige Session
                // zentral beenden.
                sessionManager.invalidateSession(
                    accessToken:
                        session.accessToken
                )


            } catch is CancellationError {

                return


            } catch {

                requestError =
                    error.localizedDescription
            }
        }
    }
}


// MARK: - Unified Password Field

private struct UnifiedChangePasswordField: View {

    let title: String

    let placeholder: String

    @Binding
    var value: String

    let borderColor: Color?

    let textContentType: UITextContentType

    @State
    private var isVisible = false

    @FocusState
    private var isFocused: Bool


    var body: some View {

        VStack(
            alignment: .leading,
            spacing: 8
        ) {

            Text(
                title
            )
            .font(
                .system(
                    size: 15,
                    weight: .semibold
                )
            )


            HStack(
                spacing: 12
            ) {

                Image(
                    systemName:
                        "lock.fill"
                )
                .font(
                    .system(
                        size: 14,
                        weight: .medium
                    )
                )
                .foregroundStyle(
                    isFocused
                        ? Color.accentColor
                        : Color.secondary
                )
                .frame(
                    width: 22
                )


                Group {

                    if isVisible {

                        TextField(
                            "",
                            text:
                                $value,
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
                            text:
                                $value,
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
                    textContentType
                )
                .textInputAutocapitalization(
                    .never
                )
                .autocorrectionDisabled()
                .focused(
                    $isFocused
                )


                Button {

                    isVisible.toggle()

                    isFocused =
                        true

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
                }
                .buttonStyle(
                    .plain
                )
            }
            .padding(
                .horizontal,
                16
            )
            .frame(
                minHeight: 56
            )
            .background(
                Color(
                    uiColor:
                        .systemBackground
                )
                .opacity(
                    0.92
                ),
                in:
                    RoundedRectangle(
                        cornerRadius: 14,
                        style: .continuous
                    )
            )
            .overlay {

                RoundedRectangle(
                    cornerRadius: 14,
                    style: .continuous
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
                            0.12
                        )
                        : Color.black.opacity(
                            0.04
                        ),
                radius:
                    isFocused
                        ? 8
                        : 4,
                y:
                    isFocused
                        ? 5
                        : 2
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
