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
    case creationRejected(String)
    case creationUncertain
    case updateRejected(String)
    case updateUncertain
    case notFound

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
        case .creationRejected(let message):
            return message
        case .creationUncertain:
            return "Die Erstellung konnte nicht bestätigt werden. "
                + "Das Lernmodul wurde möglicherweise bereits gespeichert. "
                + "Bitte prüfe die Übersicht, bevor du erneut speicherst."
        case .updateRejected(let message):
            return message
        case .updateUncertain:
            return "Die Änderung konnte nicht bestätigt werden. "
                + "Sie wurde möglicherweise bereits gespeichert. "
                + "Bitte prüfe die Übersicht, bevor du erneut speicherst."
        case .notFound:
            return "Dieses Lernmodul ist nicht mehr verfügbar. "
                + "Bitte aktualisiere die Übersicht."
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
struct CreateModuleRequest: Encodable {
    let name: String
    let code: String?
    let description: String?
    let color: String?
}

private struct ModuleProblem: Decodable {
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

extension ModuleService {
    func create(
        _ input: CreateModuleRequest,
        accessToken: String
    ) async throws -> StudyModule {
        guard let baseURL = APIConfiguration.baseURL else {
            throw ModuleServiceError.configuration
        }

        var request = URLRequest(
            url: baseURL.appendingPathComponent("api/modules/")
        )
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
        request.setValue(
            "Bearer \(accessToken)",
            forHTTPHeaderField: "Authorization"
        )
        request.httpBody = try JSONEncoder().encode(input)

        let configuration = URLSessionConfiguration.ephemeral
        configuration.urlCache = nil
        configuration.httpCookieStorage = nil

        let client = URLSession(configuration: configuration)
        defer { client.finishTasksAndInvalidate() }

        try Task.checkCancellation()

        let data: Data
        let response: URLResponse

        do {
            (data, response) = try await client.data(for: request)
        } catch {
            // Bei einem Verbindungsabbruch könnte der Server
            // das Modul bereits gespeichert haben.
            throw ModuleServiceError.creationUncertain
        }

        guard let httpResponse = response as? HTTPURLResponse else {
            throw ModuleServiceError.creationUncertain
        }

        if httpResponse.statusCode == 401 {
            throw ModuleServiceError.unauthorized
        }

        // Serverfehler können auch nach einer Speicherung auftreten.
        if httpResponse.statusCode >= 500
            || httpResponse.statusCode == 408 {
            throw ModuleServiceError.creationUncertain
        }

        guard (200..<300).contains(httpResponse.statusCode) else {
            let problem = try? JSONDecoder().decode(
                ModuleProblem.self,
                from: data
            )

            throw ModuleServiceError.creationRejected(
                problem?.message
                    ?? "Das Lernmodul konnte nicht erstellt werden "
                    + "(HTTP \(httpResponse.statusCode))."
            )
        }

        guard let module = try? JSONDecoder().decode(
            StudyModule.self,
            from: data
        ),
        !module.id.isEmpty,
        !module.name.trimmingCharacters(
            in: .whitespacesAndNewlines
        ).isEmpty else {
            throw ModuleServiceError.creationUncertain
        }

        return module
    }
}
struct UpdateModuleRequest: Encodable {
    let name: String
    let code: String?
    let description: String?
    let color: String?

    enum CodingKeys: String, CodingKey {
        case name, code, description, color
    }

    func encode(to encoder: Encoder) throws {
        var container = encoder.container(keyedBy: CodingKeys.self)
        try container.encode(name, forKey: .name)
        try container.encode(code, forKey: .code)
        try container.encode(description, forKey: .description)
        try container.encode(color, forKey: .color)
    }
}

extension ModuleService {
    func update(
        moduleId: String,
        input: UpdateModuleRequest,
        accessToken: String
    ) async throws -> StudyModule {
        guard let baseURL = APIConfiguration.baseURL else {
            throw ModuleServiceError.configuration
        }

        guard let requestedID = UUID(uuidString: moduleId) else {
            throw ModuleServiceError.notFound
        }

        let url = baseURL
            .appendingPathComponent("api/modules")
            .appendingPathComponent(requestedID.uuidString)

        var request = URLRequest(url: url)
        request.httpMethod = "PUT"
        request.timeoutInterval = 20
        request.setValue(
            "application/json",
            forHTTPHeaderField: "Content-Type"
        )
        request.setValue(
            "application/json",
            forHTTPHeaderField: "Accept"
        )
        request.setValue(
            "Bearer \(accessToken)",
            forHTTPHeaderField: "Authorization"
        )
        request.httpBody = try JSONEncoder().encode(input)

        let configuration = URLSessionConfiguration.ephemeral
        configuration.urlCache = nil
        configuration.httpCookieStorage = nil

        let client = URLSession(configuration: configuration)
        defer { client.finishTasksAndInvalidate() }

        try Task.checkCancellation()

        let data: Data
        let response: URLResponse

        do {
            (data, response) = try await client.data(for: request)
        } catch {
            throw ModuleServiceError.updateUncertain
        }

        guard let httpResponse = response as? HTTPURLResponse else {
            throw ModuleServiceError.updateUncertain
        }

        if httpResponse.statusCode == 401 {
            throw ModuleServiceError.unauthorized
        }

        if httpResponse.statusCode == 404 {
            throw ModuleServiceError.notFound
        }

        if httpResponse.statusCode >= 500
            || httpResponse.statusCode == 408 {
            throw ModuleServiceError.updateUncertain
        }

        guard (200..<300).contains(httpResponse.statusCode) else {
            let problem = try? JSONDecoder().decode(
                ModuleProblem.self,
                from: data
            )

            throw ModuleServiceError.updateRejected(
                problem?.message
                    ?? "Das Lernmodul konnte nicht geändert werden "
                    + "(HTTP \(httpResponse.statusCode))."
            )
        }

        guard let module = try? JSONDecoder().decode(
            StudyModule.self,
            from: data
        ),
        UUID(uuidString: module.id) == requestedID,
        !module.name.trimmingCharacters(
            in: .whitespacesAndNewlines
        ).isEmpty else {
            throw ModuleServiceError.updateUncertain
        }

        return module
    }
}
