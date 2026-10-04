import SwiftUI

struct FirstLaunchView: View {
    let onContinue: () -> Void

    var body: some View {
        ScrollView {
            VStack(spacing: 0) {

                Spacer()
                    .frame(height: 48)

                // MARK: - App-Symbol

                Image(systemName: "graduationcap.fill")
                    .font(
                        .system(
                            size: 40,
                            weight: .semibold
                        )
                    )
                    .foregroundStyle(.tint)
                    .frame(
                        width: 88,
                        height: 88
                    )
                    .background(
                        Color.accentColor.opacity(0.11),
                        in: RoundedRectangle(
                            cornerRadius: 22,
                            style: .continuous
                        )
                    )
                    .accessibilityHidden(true)

                // MARK: - Branding

                Text("Study Organizer")
                    .font(
                        .system(
                            size: 20,
                            weight: .semibold
                        )
                    )
                    .foregroundStyle(.tint)
                    .padding(.top, 14)

                // MARK: - Hauptbotschaft

                VStack(spacing: 12) {
                    Text("Dein Studium\nbeginnt hier.")
                        .font(
                            .system(
                                size: 34,
                                weight: .bold
                            )
                        )
                        .multilineTextAlignment(.center)
                        .tracking(-0.5)

                    Text(
                        "Plane deine Kurse, Aufgaben und Termine an einem Ort."
                    )
                    .font(.system(size: 17))
                    .foregroundStyle(.secondary)
                    .multilineTextAlignment(.center)
                    .padding(.horizontal, 8)
                }
                .padding(.top, 30)

                // MARK: - Illustration

                Image("firstLaunchIllustration")
                    .resizable()
                    .scaledToFit()
                    .frame(maxWidth: 360)
                    .padding(.top, 26)
                    .accessibilityHidden(true)

                Spacer()
                    .frame(height: 28)

                // MARK: - Loslegen

                Button {
                    onContinue()
                } label: {
                    HStack(spacing: 10) {
                        Text("Loslegen")
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

                Spacer()
                    .frame(height: 26)
            }
            .frame(maxWidth: 480)
            .frame(maxWidth: .infinity)
            .padding(.horizontal, 24)
        }
        .background(
            Color(uiColor: .systemGroupedBackground)
                .ignoresSafeArea()
        )
    }
}

#Preview {
    FirstLaunchView {
        print("Loslegen")
    }
}
