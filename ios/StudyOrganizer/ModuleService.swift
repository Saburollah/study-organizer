import Foundation

struct StudyModule: Decodable, Identifiable {
    let id: String
    let name: String
    let code: String?
    let description: String?
    let color: String?
    let createdAtUtc: String
    let isExternalCourseLinked: Bool
}

enum ModuleServiceError: LocalizedError {
    case configuration
    case unauthorized
    case server(Int)
    case invalidResponse
    case network
    case timeout

    var errorDescription: String? {
        switch self {
        case .configuration:
            return "Für diese App-Version ist noch kein Server eingerichtet."
        case .unauthorized:
            return "Deine Sitzung ist nicht mehr gültig. "
                + "Bitte melde dich erneut an."
        case .server(let status):
            return "Die Lernmodule konnten nicht geladen werden "
                + "(HTTP \(status)). Bitte versuche es erneut."
        case .invalidResponse:
            return "Die Antwort des Servers konnte nicht gelesen werden."
        case .network:
            return "Der Server ist nicht erreichbar. "
                + "Bitte prüfe deine Verbindung."
        case .timeout:
            return "Der Server hat nicht rechtzeitig geantwortet. "
                + "Bitte versuche es erneut."
        }
    }
}

struct ModuleService {
    func getAll(accessToken: String) async throws -> [StudyModule] {
        guard let baseURL = APIConfiguration.baseURL else {
            throw ModuleServiceError.configuration
        }

        let url = baseURL.appendingPathComponent("api/modules/")

        var request = URLRequest(url: url)
        request.httpMethod = "GET"
        request.timeoutInterval = 20
        request.cachePolicy = .reloadIgnoringLocalCacheData
        request.setValue(
            "application/json",
            forHTTPHeaderField: "Accept"
        )
        request.setValue(
            "Bearer \(accessToken)",
            forHTTPHeaderField: "Authorization"
        )

        // Keine dauerhafte Speicherung der Antworten oder Cookies.
        let configuration = URLSessionConfiguration.ephemeral
        configuration.urlCache = nil
        configuration.httpCookieStorage = nil

        let client = URLSession(configuration: configuration)
        defer { client.finishTasksAndInvalidate() }

        let data: Data
        let response: URLResponse

        do {
            (data, response) = try await client.data(for: request)
        } catch let error as URLError {
            if error.code == .cancelled {
                throw CancellationError()
            }
            if error.code == .timedOut {
                throw ModuleServiceError.timeout
            }
            throw ModuleServiceError.network
        }

        try Task.checkCancellation()

        guard let httpResponse = response as? HTTPURLResponse else {
            throw ModuleServiceError.invalidResponse
        }

        if httpResponse.statusCode == 401 {
            throw ModuleServiceError.unauthorized
        }

        guard (200..<300).contains(httpResponse.statusCode) else {
            throw ModuleServiceError.server(httpResponse.statusCode)
        }

        guard let modules = try? JSONDecoder().decode(
            [StudyModule].self,
            from: data
        ) else {
            throw ModuleServiceError.invalidResponse
        }

        return modules
    }
}
