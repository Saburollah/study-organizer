import SwiftUI

@main
struct StudyOrganizerApp: App {
    @StateObject private var sessionManager = SessionManager()
    @Environment(\.scenePhase) private var scenePhase

    var body: some Scene {
        WindowGroup {
            Group {
                if let session = sessionManager.session {
                    NavigationStack {
                        VStack(spacing: 24) {
                            Text("Angemeldet")
                                .font(.largeTitle.bold())

                            Text(session.email)
                                .multilineTextAlignment(.center)

                            Button("Abmelden") {
                                sessionManager.signOut()
                            }
                            .buttonStyle(.borderedProminent)
                        }
                        .padding(24)
                    }
                } else {
                    ContentView()
                }
            }
            .environmentObject(sessionManager)
            .alert(
                "Hinweis",
                isPresented: Binding(
                    get: { sessionManager.errorMessage != nil },
                    set: { isPresented in
                        if !isPresented {
                            sessionManager.errorMessage = nil
                        }
                    }
                )
            ) {
                Button("OK", role: .cancel) {
                    sessionManager.errorMessage = nil
                }
            } message: {
                Text(sessionManager.errorMessage ?? "")
            }
            .onChange(of: scenePhase) {
                if scenePhase == .active {
                    sessionManager.checkExpiration()
                }
            }
        }
    }
}
