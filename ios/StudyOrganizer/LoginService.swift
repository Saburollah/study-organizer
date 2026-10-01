import Foundation

struct LoginResponse: Decodable {
    let accessToken: String
    let expiresAtUtc: String

    var expirationDate: Date? {
        let formatter = ISO8601DateFormatter()
        formatter.formatOptions = [
            .withInternetDateTime,
            .withFractionalSeconds
        ]

        if let date = formatter.date(from: expiresAtUtc) {
            return date
        }

        formatter.formatOptions = [.withInternetDateTime]
        return formatter.date(from: expiresAtUtc)
    }
}

private struct LoginRequest: Encodable {
    let email: String
    let password: String
}

private struct LoginProblem: Decodable {
    let title: String?
    let detail: String?
    let errors: [String: [String]]?

    var message: String? {
        let candidates =
            (errors ?? [:]).sorted { $0.key < $1.key }
                .flatMap { $0.value }
            + [detail, title].compactMap { $0 }

        return candidates.first {
            !$0.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty
        }
    }
}

enum LoginError: LocalizedError {
    case configuration
    case invalidCredentials
    case server(String)
    case invalidResponse
    case network
    case timeout

    var errorDescription: String? {
        switch self {
        case .configuration:
            return "Für diese App-Version ist noch kein Server eingerichtet."
        case .invalidCredentials:
            return "E-Mail-Adresse oder Passwort ist falsch."
        case .server(let message):
            return message
        case .invalidResponse:
            return "Die Anmeldung konnte wegen einer ungültigen "
                + "Serverantwort nicht abgeschlossen werden."
        case .network:
            return "Der Server ist nicht erreichbar. "
                + "Bitte prüfe deine Verbindung und versuche es erneut."
        case .timeout:
            return "Der Server hat nicht rechtzeitig geantwortet. "
                + "Bitte versuche es erneut."
        }
    }
}

struct LoginService {
    func login(
        email: String,
        password: String
    ) async throws -> LoginResponse {
        guard let baseURL = APIConfiguration.baseURL else {
            throw LoginError.configuration
        }

        let url = baseURL.appendingPathComponent("api/auth/login")

        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.timeoutInterval = 20
        request.setValue(
            "application/json",
            forHTTPHeaderField: "Content-Type"
        )
        request.setValue(
            "application/json",
            forHTTPHeaderField: "Accept"
        )
        request.httpBody = try JSONEncoder().encode(
            LoginRequest(
                email: email.trimmingCharacters(in: .whitespacesAndNewlines),
                password: password
            )
        )

        let data: Data
        let response: URLResponse

        do {
            (data, response) = try await URLSession.shared.data(for: request)
        } catch let error as URLError {
            if error.code == .cancelled {
                throw CancellationError()
            }
            if error.code == .timedOut {
                throw LoginError.timeout
            }
            throw LoginError.network
        }

        guard let httpResponse = response as? HTTPURLResponse else {
            throw LoginError.invalidResponse
        }

        if httpResponse.statusCode == 401 {
            throw LoginError.invalidCredentials
        }

        guard (200..<300).contains(httpResponse.statusCode) else {
            let problem = try? JSONDecoder().decode(
                LoginProblem.self,
                from: data
            )

            throw LoginError.server(
                problem?.message
                    ?? "Die Anmeldung ist fehlgeschlagen "
                    + "(HTTP \(httpResponse.statusCode))."
            )
        }

        guard let result = try? JSONDecoder().decode(
            LoginResponse.self,
            from: data
        ),
        !result.accessToken.trimmingCharacters(
            in: .whitespacesAndNewlines
        ).isEmpty,
        let expirationDate = result.expirationDate,
        expirationDate > Date() else {
            throw LoginError.invalidResponse
        }

        return result
    }
}
