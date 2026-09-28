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
                            AuthenticationPlaceholderView(
                                title: "Anmelden",
                                message: "Hier kannst du dich bald anmelden."
                            )
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
                                AuthenticationPlaceholderView(
                                    title: "Registrieren",
                                    message: "Hier kannst du bald ein Konto erstellen."
                                )
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

#Preview {
    ContentView()
}
