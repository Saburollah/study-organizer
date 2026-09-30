import Foundation

struct RegistrationResponse: Decodable {
    let userId: String
    let email: String
}

private struct RegistrationRequest: Encodable {
    let email: String
    let password: String
}

private struct RegistrationProblem: Decodable {
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

enum RegistrationError: LocalizedError {
    case configuration
    case server(String)
    case invalidResponse
    case network
    case timeout

    var errorDescription: String? {
        switch self {
        case .configuration:
            return "Für diese App-Version ist noch kein Server eingerichtet."
        case .server(let message):
            return message
        case .invalidResponse:
            return "Die Serverantwort konnte nicht bestätigt werden. "
                + "Das Konto wurde möglicherweise bereits erstellt."
        case .network:
            return "Der Server ist nicht erreichbar oder die Verbindung "
                + "wurde unterbrochen. Bitte prüfe deine Verbindung. "
                + "Das Konto wurde möglicherweise bereits erstellt."
        case .timeout:
            return "Der Server hat nicht rechtzeitig geantwortet. "
                + "Das Konto wurde möglicherweise bereits erstellt."
        }
    }
}

private enum RegistrationAPIConfiguration {
    static var baseURL: URL? {
        #if DEBUG && targetEnvironment(simulator)
        return URL(string: "http://localhost:5101")
        #else
        // Eine produktive HTTPS-Adresse wird später eingerichtet.
        return nil
        #endif
    }
}

struct RegistrationService {
    func register(
        email: String,
        password: String
    ) async throws -> RegistrationResponse {
        guard let baseURL = RegistrationAPIConfiguration.baseURL else {
            throw RegistrationError.configuration
        }

        let url = baseURL.appendingPathComponent("api/auth/register")

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
            RegistrationRequest(
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
                throw RegistrationError.timeout
            }
            throw RegistrationError.network
        }

        guard let httpResponse = response as? HTTPURLResponse else {
            throw RegistrationError.invalidResponse
        }

        guard (200..<300).contains(httpResponse.statusCode) else {
            let problem = try? JSONDecoder().decode(
                RegistrationProblem.self,
                from: data
            )

            throw RegistrationError.server(
                problem?.message
                    ?? "Die Registrierung ist fehlgeschlagen "
                    + "(HTTP \(httpResponse.statusCode))."
            )
        }

        guard let result = try? JSONDecoder().decode(
            RegistrationResponse.self,
            from: data
        ),
        !result.userId.isEmpty,
        !result.email.isEmpty else {
            throw RegistrationError.invalidResponse
        }

        return result
    }
}
