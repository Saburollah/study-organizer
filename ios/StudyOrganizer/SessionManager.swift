import SwiftUI
import Combine

@MainActor
final class SessionManager: ObservableObject {
    @Published private(set) var session: AuthSession?
    @Published var errorMessage: String?

    private let store = SessionStore()
    private var expirationTask: Task<Void, Never>?

    init() {
        restore()
    }

    func signIn(email: String, password: String) async throws {
        let response = try await LoginService().login(
            email: email,
            password: password
        )

        try Task.checkCancellation()

        guard let expirationDate = response.expirationDate else {
            throw LoginError.invalidResponse
        }

        let newSession = AuthSession(
            email: email.trimmingCharacters(in: .whitespacesAndNewlines),
            accessToken: response.accessToken,
            expiresAt: expirationDate
        )

        // Erst sicher speichern, dann die angemeldete Ansicht zeigen.
        try store.save(newSession)

        errorMessage = nil
        session = newSession
        scheduleExpiration()
    }

    func restore() {
        expirationTask?.cancel()

        do {
            session = try store.load()
            errorMessage = nil
            scheduleExpiration()
        } catch {
            session = nil
            errorMessage = error.localizedDescription
        }
    }

    func signOut() {
        do {
            // Bei einem Löschfehler keine erfolgreiche Abmeldung vortäuschen.
            try store.delete()
            expirationTask?.cancel()
            session = nil
            errorMessage = nil
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func checkExpiration() {
        guard let session, !session.isValid else { return }

        expirationTask?.cancel()
        self.session = nil

        do {
            try store.delete()
            errorMessage = "Deine Sitzung ist abgelaufen. "
                + "Bitte melde dich erneut an."
        } catch {
            errorMessage = error.localizedDescription
        }
    }
    
    func invalidateSession(accessToken: String) {
        // Eine verspätete Antwort darf keine neuere Sitzung beenden.
        guard session?.accessToken == accessToken else { return }

        expirationTask?.cancel()
        session = nil

        do {
            try store.delete()
            errorMessage = "Deine Sitzung ist nicht mehr gültig. "
                + "Bitte melde dich erneut an."
        } catch {
            errorMessage = "Deine Sitzung wurde beendet, konnte aber "
                + "nicht aus der sicheren Speicherung entfernt werden. "
                + error.localizedDescription
        }
    }

    private func scheduleExpiration() {
        expirationTask?.cancel()

        guard let expirationDate = session?.expiresAt else { return }

        expirationTask = Task { @MainActor [weak self] in
            while !Task.isCancelled {
                let remaining = expirationDate.timeIntervalSinceNow

                if remaining <= 0 {
                    self?.checkExpiration()
                    return
                }

                // Regelmäßig neu berechnen, auch nach Änderungen der Uhrzeit.
                let delay = min(remaining, 30)

                do {
                    try await Task.sleep(
                        nanoseconds: UInt64(delay * 1_000_000_000)
                    )
                } catch {
                    return
                }
            }
        }
    }
}
