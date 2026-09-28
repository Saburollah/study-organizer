import SwiftUI

struct ContentView: View {
    var body: some View {
        NavigationStack {
            ScrollView {
                VStack(spacing: 32) {
                    Image(systemName: "book.closed.fill")
                        .font(.largeTitle)
                        .foregroundStyle(.tint)
                        .accessibilityHidden(true)

                    VStack(spacing: 12) {
                        Text("Study Organizer")
                            .font(.largeTitle.bold())

                        Text("Willkommen! Organisiere dein Studium an einem Ort.")
                            .font(.body)
                            .foregroundStyle(.secondary)
                    }
                    .multilineTextAlignment(.center)

                    VStack(spacing: 16) {
                        NavigationLink {
                            AuthenticationPlaceholderView(
                                title: "Anmelden",
                                message: "Hier kannst du dich bald anmelden."
                            )
                        } label: {
                            Text("Anmelden")
                                .frame(maxWidth: .infinity, minHeight: 44)
                        }
                        .buttonStyle(.borderedProminent)

                        NavigationLink {
                            AuthenticationPlaceholderView(
                                title: "Registrieren",
                                message: "Hier kannst du bald ein Konto erstellen."
                            )
                        } label: {
                            Text("Registrieren")
                                .frame(maxWidth: .infinity, minHeight: 44)
                        }
                        .buttonStyle(.bordered)
                    }
                }
                .frame(maxWidth: 480)
                .frame(maxWidth: .infinity)
                .padding(24)
            }
            .navigationTitle("Startseite")
            .navigationBarTitleDisplayMode(.inline)
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
