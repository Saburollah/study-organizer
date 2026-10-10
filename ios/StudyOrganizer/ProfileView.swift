import SwiftUI

struct ProfileView: View {

    let session: AuthSession

    @EnvironmentObject
    private var sessionManager: SessionManager

    @State
    private var showEditProfile = false

    @State
    private var showChangePassword = false

    @State
    private var showLogoutConfirmation = false


    var body: some View {

        ScrollView {

            VStack(
                alignment: .leading,
                spacing: 22
            ) {

                profileHeader

                personalDataSection

                securitySection

                logoutSection

                appFooter
            }
            .frame(
                maxWidth: 620
            )
            .frame(
                maxWidth: .infinity
            )
            .padding(
                .horizontal,
                18
            )
            .padding(
                .top,
                8
            )
            .padding(
                .bottom,
                125
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
                        0.035
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
        .navigationTitle(
            "Profil"
        )
        .navigationBarTitleDisplayMode(
            .large
        )

        // MARK: - Persönliche Daten

        .sheet(
            isPresented:
                $showEditProfile
        ) {

            NavigationStack {

                EditProfileView(
                    session: session
                )
                .environmentObject(
                    sessionManager
                )
            }
        }

        // MARK: - Passwort ändern

        .sheet(
            isPresented:
                $showChangePassword
        ) {

            NavigationStack {

                ChangePasswordView(
                    session: session
                )
                .environmentObject(
                    sessionManager
                )
            }
        }

        // MARK: - Logout

        .alert(
            "Abmelden?",
            isPresented:
                $showLogoutConfirmation
        ) {

            Button(
                "Abbrechen",
                role:
                    .cancel
            ) {}


            Button(
                "Abmelden",
                role:
                    .destructive
            ) {

                sessionManager.signOut()
            }

        } message: {

            Text(
                "Möchtest du dich wirklich von deinem Konto abmelden?"
            )
        }
    }


    // MARK: - Profile Header

    private var profileHeader: some View {

        VStack(
            spacing: 12
        ) {

            ZStack {

                Circle()
                    .fill(
                        LinearGradient(
                            colors: [
                                Color.accentColor,
                                Color.accentColor.opacity(
                                    0.72
                                )
                            ],
                            startPoint:
                                .topLeading,
                            endPoint:
                                .bottomTrailing
                        )
                    )
                    .frame(
                        width: 76,
                        height: 76
                    )
                    .shadow(
                        color:
                            Color.accentColor.opacity(
                                0.28
                            ),
                        radius: 14,
                        x: 0,
                        y: 8
                    )


                Circle()
                    .stroke(
                        Color.white.opacity(
                            0.48
                        ),
                        lineWidth: 1.2
                    )
                    .frame(
                        width: 74,
                        height: 74
                    )


                Circle()
                    .fill(
                        LinearGradient(
                            colors: [
                                Color.white.opacity(
                                    0.28
                                ),
                                Color.clear
                            ],
                            startPoint:
                                .top,
                            endPoint:
                                .center
                        )
                    )
                    .frame(
                        width: 66,
                        height: 66
                    )
                    .offset(
                        y: -6
                    )


                Image(
                    systemName:
                        "person.fill"
                )
                .font(
                    .system(
                        size: 29,
                        weight: .semibold
                    )
                )
                .foregroundStyle(
                    .white
                )
            }


            Text(
                session.email
            )
            .font(
                .system(
                    size: 18,
                    weight: .semibold
                )
            )
            .foregroundStyle(
                .primary
            )
            .lineLimit(
                1
            )
            .minimumScaleFactor(
                0.8
            )


            HStack(
                spacing: 6
            ) {

                Circle()
                    .fill(
                        Color.green
                    )
                    .frame(
                        width: 7,
                        height: 7
                    )
                    .shadow(
                        color:
                            Color.green.opacity(
                                0.55
                            ),
                        radius: 4
                    )


                Text(
                    "Angemeldet"
                )
                .font(
                    .system(
                        size: 13,
                        weight: .medium
                    )
                )
                .foregroundStyle(
                    .secondary
                )
            }
        }
        .frame(
            maxWidth: .infinity
        )
        .padding(
            .vertical,
            18
        )
    }


    // MARK: - Personal Data

    private var personalDataSection: some View {

        VStack(
            alignment: .leading,
            spacing: 10
        ) {

            sectionTitle(
                "PERSÖNLICHE DATEN"
            )


            Button {

                showEditProfile =
                    true

            } label: {

                HStack(
                    spacing: 12
                ) {

                    rowIcon(
                        "person.text.rectangle.fill"
                    )


                    VStack(
                        alignment: .leading,
                        spacing: 3
                    ) {

                        Text(
                            "Persönliche Daten"
                        )
                        .font(
                            .system(
                                size: 16,
                                weight: .medium
                            )
                        )
                        .foregroundStyle(
                            .primary
                        )


                        Text(
                            "Name, Geburtstag und Geschlecht"
                        )
                        .font(
                            .system(
                                size: 12.5
                            )
                        )
                        .foregroundStyle(
                            .secondary
                        )
                    }


                    Spacer()


                    Image(
                        systemName:
                            "chevron.right"
                    )
                    .font(
                        .system(
                            size: 13,
                            weight: .semibold
                        )
                    )
                    .foregroundStyle(
                        .tertiary
                    )
                }
                .padding(
                    .horizontal,
                    16
                )
                .frame(
                    minHeight: 68
                )
                .background(
                    cardBackground
                )
            }
            .buttonStyle(
                .plain
            )
        }
    }


    // MARK: - Security

    private var securitySection: some View {

        VStack(
            alignment: .leading,
            spacing: 10
        ) {

            sectionTitle(
                "SICHERHEIT"
            )


            Button {

                showChangePassword =
                    true

            } label: {

                HStack(
                    spacing: 12
                ) {

                    rowIcon(
                        "lock.fill"
                    )


                    VStack(
                        alignment: .leading,
                        spacing: 3
                    ) {

                        Text(
                            "Passwort ändern"
                        )
                        .font(
                            .system(
                                size: 16,
                                weight: .medium
                            )
                        )
                        .foregroundStyle(
                            .primary
                        )


                        Text(
                            "Lege ein neues Passwort fest."
                        )
                        .font(
                            .system(
                                size: 12.5
                            )
                        )
                        .foregroundStyle(
                            .secondary
                        )
                    }


                    Spacer()


                    Image(
                        systemName:
                            "chevron.right"
                    )
                    .font(
                        .system(
                            size: 13,
                            weight: .semibold
                        )
                    )
                    .foregroundStyle(
                        .tertiary
                    )
                }
                .padding(
                    .horizontal,
                    16
                )
                .frame(
                    minHeight: 68
                )
                .background(
                    cardBackground
                )
            }
            .buttonStyle(
                .plain
            )
        }
    }


    // MARK: - Logout

    private var logoutSection: some View {

        VStack(
            alignment: .leading,
            spacing: 10
        ) {

            sectionTitle(
                "KONTO"
            )


            Button {

                showLogoutConfirmation =
                    true

            } label: {

                HStack(
                    spacing: 10
                ) {

                    Spacer()


                    Image(
                        systemName:
                            "rectangle.portrait.and.arrow.right"
                    )
                    .font(
                        .system(
                            size: 17,
                            weight: .semibold
                        )
                    )


                    Text(
                        "Abmelden"
                    )
                    .font(
                        .system(
                            size: 16,
                            weight: .semibold
                        )
                    )


                    Spacer()
                }
                .foregroundStyle(
                    .red
                )
                .frame(
                    minHeight: 60
                )
                .background(
                    logoutBackground
                )
            }
            .buttonStyle(
                .plain
            )
        }
    }


    // MARK: - Footer

    private var appFooter: some View {

        VStack(
            spacing: 4
        ) {

            Text(
                "Version \(appVersion)"
            )
            .font(
                .system(
                    size: 12.5,
                    weight: .medium
                )
            )
            .foregroundStyle(
                .secondary
            )


            Text(
                "© 2026 Study Organizer"
            )
            .font(
                .system(
                    size: 12
                )
            )
            .foregroundStyle(
                .tertiary
            )
        }
        .frame(
            maxWidth: .infinity
        )
        .padding(
            .top,
            3
        )
    }


    // MARK: - Section Title

    private func sectionTitle(
        _ title: String
    ) -> some View {

        Text(
            title
        )
        .font(
            .system(
                size: 11,
                weight: .bold
            )
        )
        .tracking(
            1.35
        )
        .foregroundStyle(
            LinearGradient(
                colors: [
                    Color.accentColor,
                    Color.accentColor.opacity(
                        0.75
                    )
                ],
                startPoint:
                    .leading,
                endPoint:
                    .trailing
            )
        )
        .padding(
            .leading,
            4
        )
    }


    // MARK: - Row Icon

    private func rowIcon(
        _ systemName: String
    ) -> some View {

        Image(
            systemName:
                systemName
        )
        .font(
            .system(
                size: 15,
                weight: .semibold
            )
        )
        .foregroundStyle(
            Color.accentColor
        )
        .frame(
            width: 36,
            height: 36
        )
        .background(
            LinearGradient(
                colors: [
                    Color.accentColor.opacity(
                        0.17
                    ),
                    Color.accentColor.opacity(
                        0.06
                    )
                ],
                startPoint:
                    .topLeading,
                endPoint:
                    .bottomTrailing
            ),
            in:
                RoundedRectangle(
                    cornerRadius: 10,
                    style: .continuous
                )
        )
        .overlay {

            RoundedRectangle(
                cornerRadius: 10,
                style: .continuous
            )
            .stroke(
                Color.white.opacity(
                    0.45
                ),
                lineWidth: 0.8
            )
        }
        .shadow(
            color:
                Color.accentColor.opacity(
                    0.10
                ),
            radius: 5,
            x: 0,
            y: 3
        )
    }


    // MARK: - Premium Card Background

    private var cardBackground: some View {

        RoundedRectangle(
            cornerRadius: 20,
            style: .continuous
        )
        .fill(
            Color(
                uiColor:
                    .systemBackground
            )
            .opacity(
                0.90
            )
        )
        .background {

            RoundedRectangle(
                cornerRadius: 20,
                style: .continuous
            )
            .fill(
                .ultraThinMaterial
            )
        }
        .overlay {

            RoundedRectangle(
                cornerRadius: 20,
                style: .continuous
            )
            .stroke(
                LinearGradient(
                    colors: [
                        Color.white.opacity(
                            0.70
                        ),
                        Color.secondary.opacity(
                            0.08
                        )
                    ],
                    startPoint:
                        .topLeading,
                    endPoint:
                        .bottomTrailing
                ),
                lineWidth: 1
            )
        }
        .shadow(
            color:
                Color.black.opacity(
                    0.055
                ),
            radius: 14,
            x: 0,
            y: 8
        )
    }


    // MARK: - Logout Background

    private var logoutBackground: some View {

        RoundedRectangle(
            cornerRadius: 20,
            style: .continuous
        )
        .fill(
            Color(
                uiColor:
                    .systemBackground
            )
            .opacity(
                0.92
            )
        )
        .background {

            RoundedRectangle(
                cornerRadius: 20,
                style: .continuous
            )
            .fill(
                .ultraThinMaterial
            )
        }
        .overlay {

            RoundedRectangle(
                cornerRadius: 20,
                style: .continuous
            )
            .stroke(
                LinearGradient(
                    colors: [
                        Color.white.opacity(
                            0.55
                        ),
                        Color.red.opacity(
                            0.12
                        )
                    ],
                    startPoint:
                        .topLeading,
                    endPoint:
                        .bottomTrailing
                ),
                lineWidth: 1
            )
        }
        .shadow(
            color:
                Color.red.opacity(
                    0.045
                ),
            radius: 12,
            x: 0,
            y: 7
        )
    }


    // MARK: - App Version

    private var appVersion: String {

        Bundle.main.object(
            forInfoDictionaryKey:
                "CFBundleShortVersionString"
        ) as? String
        ?? "1.0"
    }
}
