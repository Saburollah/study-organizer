import Foundation
import Security

struct AuthSession: Codable {
    let email: String
    let accessToken: String
    let expiresAt: Date

    var isValid: Bool {
        !email.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty
            && !accessToken.trimmingCharacters(
                in: .whitespacesAndNewlines
            ).isEmpty
            && expiresAt > Date()
    }
}

enum SessionStoreError: LocalizedError {
    case keychain(OSStatus)
    case invalidData

    var errorDescription: String? {
        switch self {
        case .keychain(let status):
            return "Die sichere Sitzungsspeicherung ist fehlgeschlagen "
                + "(Code \(status)). Bitte versuche es erneut."
        case .invalidData:
            return "Die gespeicherte Sitzung ist ungültig. "
                + "Bitte melde dich erneut an."
        }
    }
}

struct SessionStore {
    private var service: String {
        let app = Bundle.main.bundleIdentifier ?? "StudyOrganizer"
        let server = APIConfiguration.baseURL?.absoluteString
            ?? "unconfigured"

        return "\(app).session.\(server)"
    }

    private var query: [String: Any] {
        [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: service,
            kSecAttrAccount as String: "current-session",
            kSecAttrSynchronizable as String: false
        ]
    }

    func save(_ session: AuthSession) throws {
        guard session.isValid else {
            throw SessionStoreError.invalidData
        }

        let data = try JSONEncoder().encode(session)

        let attributes: [String: Any] = [
            kSecValueData as String: data,
            kSecAttrAccessible as String:
                kSecAttrAccessibleWhenUnlockedThisDeviceOnly
        ]

        let updateStatus = SecItemUpdate(
            query as CFDictionary,
            attributes as CFDictionary
        )

        if updateStatus == errSecItemNotFound {
            var newItem = query
            newItem.merge(attributes) { _, newValue in newValue }

            let addStatus = SecItemAdd(
                newItem as CFDictionary,
                nil
            )

            guard addStatus == errSecSuccess else {
                throw SessionStoreError.keychain(addStatus)
            }
        } else if updateStatus != errSecSuccess {
            throw SessionStoreError.keychain(updateStatus)
        }
    }

    func load() throws -> AuthSession? {
        var search = query
        search[kSecReturnData as String] = true
        search[kSecMatchLimit as String] = kSecMatchLimitOne

        var result: CFTypeRef?
        let status = SecItemCopyMatching(
            search as CFDictionary,
            &result
        )

        if status == errSecItemNotFound {
            return nil
        }

        guard status == errSecSuccess else {
            throw SessionStoreError.keychain(status)
        }

        guard let data = result as? Data,
              let session = try? JSONDecoder().decode(
                  AuthSession.self,
                  from: data
              ) else {
            try delete()
            throw SessionStoreError.invalidData
        }

        guard session.isValid else {
            try delete()
            return nil
        }

        return session
    }

    func delete() throws {
        let status = SecItemDelete(query as CFDictionary)

        guard status == errSecSuccess
                || status == errSecItemNotFound else {
            throw SessionStoreError.keychain(status)
        }
    }
}
