import Foundation

// MARK: - Aufgabe

struct StudyTask: Decodable, Identifiable {

    let id: String
    let moduleId: String

    let title: String
    let description: String?

    let dueDateUtc: String

    let status: String

    let createdAtUtc: String
    let updatedAtUtc: String?

    let externalSource: ExternalTaskSource?


    var dueDate: Date? {
        Self.parseDate(dueDateUtc)
    }


    var isCompleted: Bool {
        status.caseInsensitiveCompare(
            "Completed"
        ) == .orderedSame
    }


    var isOpen: Bool {
        status.caseInsensitiveCompare(
            "Open"
        ) == .orderedSame
    }


    private static func parseDate(
        _ value: String
    ) -> Date? {

        let formatter =
            ISO8601DateFormatter()

        formatter.formatOptions = [
            .withInternetDateTime,
            .withFractionalSeconds
        ]

        if let date =
            formatter.date(
                from: value
            ) {

            return date
        }

        formatter.formatOptions = [
            .withInternetDateTime
        ]

        return formatter.date(
            from: value
        )
    }
}


// MARK: - Externe Quelle

struct ExternalTaskSource: Decodable {

    let providerKey: String
    let courseName: String
    let sourceUrl: String?
}


// MARK: - Fehler

enum StudyTaskServiceError:
    LocalizedError {

    case configuration

    case invalidModuleId

    case unauthorized

    case moduleNotFound

    case invalidResponse

    case network

    case timeout

    case server(Int)


    var errorDescription: String? {

        switch self {

        case .configuration:

            return
                "Für diese App-Version ist noch kein Server eingerichtet."


        case .invalidModuleId:

            return
                "Das ausgewählte Lernmodul ist ungültig."


        case .unauthorized:

            return
                "Deine Sitzung ist nicht mehr gültig. "
                + "Bitte melde dich erneut an."


        case .moduleNotFound:

            return
                "Dieses Lernmodul ist nicht mehr verfügbar."


        case .invalidResponse:

            return
                "Die Aufgaben konnten nicht aus der Serverantwort gelesen werden."


        case .network:

            return
                "Der Server ist nicht erreichbar. "
                + "Bitte prüfe deine Verbindung."


        case .timeout:

            return
                "Der Server hat nicht rechtzeitig geantwortet. "
                + "Bitte versuche es erneut."


        case .server(let status):

            return
                "Die Aufgaben konnten nicht geladen werden "
                + "(HTTP \(status)). "
                + "Bitte versuche es erneut."
        }
    }
}


// MARK: - Service

struct StudyTaskService {

    func getByModule(
        moduleId: String,
        accessToken: String
    ) async throws -> [StudyTask] {

        guard let baseURL =
                APIConfiguration.baseURL
        else {

            throw
                StudyTaskServiceError
                    .configuration
        }


        guard let requestedModuleId =
                UUID(
                    uuidString: moduleId
                )
        else {

            throw
                StudyTaskServiceError
                    .invalidModuleId
        }


        let url = baseURL
            .appendingPathComponent(
                "api/modules"
            )
            .appendingPathComponent(
                requestedModuleId.uuidString
            )
            .appendingPathComponent(
                "tasks"
            )
            .appendingPathComponent(
                ""
            )


        var request =
            URLRequest(
                url: url
            )

        request.httpMethod = "GET"

        request.timeoutInterval = 20

        request.cachePolicy =
            .reloadIgnoringLocalCacheData


        request.setValue(
            "application/json",
            forHTTPHeaderField:
                "Accept"
        )


        request.setValue(
            "Bearer \(accessToken)",
            forHTTPHeaderField:
                "Authorization"
        )


        // Keine dauerhafte Speicherung
        // von Cache oder Cookies.

        let configuration =
            URLSessionConfiguration
                .ephemeral

        configuration.urlCache = nil

        configuration
            .httpCookieStorage = nil


        let client =
            URLSession(
                configuration:
                    configuration
            )


        defer {

            client
                .finishTasksAndInvalidate()
        }


        try Task
            .checkCancellation()


        let data: Data

        let response: URLResponse


        do {

            (data, response) =
                try await client.data(
                    for: request
                )

        } catch let error as URLError {

            if error.code
                == .cancelled {

                throw
                    CancellationError()
            }


            if error.code
                == .timedOut {

                throw
                    StudyTaskServiceError
                        .timeout
            }


            throw
                StudyTaskServiceError
                    .network
        }


        try Task
            .checkCancellation()


        guard let httpResponse =
                response
                    as? HTTPURLResponse
        else {

            throw
                StudyTaskServiceError
                    .invalidResponse
        }


        switch httpResponse.statusCode {

        case 200:

            break


        case 401:

            throw
                StudyTaskServiceError
                    .unauthorized


        case 404:

            throw
                StudyTaskServiceError
                    .moduleNotFound


        default:

            throw
                StudyTaskServiceError
                    .server(
                        httpResponse
                            .statusCode
                    )
        }


        guard let tasks =
                try? JSONDecoder()
                    .decode(
                        [StudyTask].self,
                        from: data
                    )
        else {

            throw
                StudyTaskServiceError
                    .invalidResponse
        }


        return tasks
    }
}
