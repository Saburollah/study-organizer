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


// MARK: - Aufgabe erstellen

struct CreateStudyTaskRequest: Encodable {

    let title: String

    let description: String?

    let dueDateUtc: String
}


// MARK: - Serverfehler

private struct StudyTaskProblem: Decodable {

    let title: String?

    let detail: String?

    let errors: [String: [String]]?


    var message: String? {

        let errorMessages =
            (errors ?? [:])
                .sorted {
                    $0.key < $1.key
                }
                .flatMap {
                    $0.value
                }


        let generalMessages =
            [detail, title]
                .compactMap {
                    $0
                }


        return
            (errorMessages + generalMessages)
                .first {

                    !$0
                        .trimmingCharacters(
                            in: .whitespacesAndNewlines
                        )
                        .isEmpty
                }
    }
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

    case creationRejected(String)

    case creationUncertain


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
                "Die Anfrage konnte nicht abgeschlossen werden "
                + "(HTTP \(status)). "
                + "Bitte versuche es erneut."


        case .creationRejected(let message):

            return message


        case .creationUncertain:

            return
                "Die Erstellung der Aufgabe konnte nicht bestätigt werden. "
                + "Die Aufgabe wurde möglicherweise bereits gespeichert. "
                + "Bitte prüfe die Aufgabenliste, bevor du erneut speicherst."
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


        request.httpMethod =
            "GET"


        request.timeoutInterval =
            20


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


        let configuration =
            URLSessionConfiguration
                .ephemeral


        configuration.urlCache =
            nil


        configuration.httpCookieStorage =
            nil


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


// MARK: - Aufgabe erstellen

extension StudyTaskService {

    func create(
        moduleId: String,
        title: String,
        description: String?,
        dueDate: Date,
        accessToken: String
    ) async throws -> StudyTask {

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


        let formatter =
            ISO8601DateFormatter()


        formatter.formatOptions = [
            .withInternetDateTime,
            .withFractionalSeconds
        ]


        let normalizedTitle =
            title.trimmingCharacters(
                in: .whitespacesAndNewlines
            )


        let normalizedDescription =
            description?
                .trimmingCharacters(
                    in: .whitespacesAndNewlines
                )


        let body =
            CreateStudyTaskRequest(
                title: normalizedTitle,
                description:
                    normalizedDescription?.isEmpty == true
                    ? nil
                    : normalizedDescription,
                dueDateUtc:
                    formatter.string(
                        from: dueDate
                    )
            )


        var request =
            URLRequest(
                url: url
            )


        request.httpMethod =
            "POST"


        request.timeoutInterval =
            20


        request.setValue(
            "application/json",
            forHTTPHeaderField:
                "Content-Type"
        )


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


        request.httpBody =
            try JSONEncoder()
                .encode(
                    body
                )


        let configuration =
            URLSessionConfiguration
                .ephemeral


        configuration.urlCache =
            nil


        configuration.httpCookieStorage =
            nil


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


            throw
                StudyTaskServiceError
                    .creationUncertain
        }


        try Task
            .checkCancellation()


        guard let httpResponse =
                response
                    as? HTTPURLResponse
        else {

            throw
                StudyTaskServiceError
                    .creationUncertain
        }


        switch httpResponse.statusCode {

        case 201:

            break


        case 400:

            let problem =
                try? JSONDecoder()
                    .decode(
                        StudyTaskProblem.self,
                        from: data
                    )


            throw
                StudyTaskServiceError
                    .creationRejected(
                        problem?.message
                        ?? "Die Aufgabe konnte nicht erstellt werden."
                    )


        case 401:

            throw
                StudyTaskServiceError
                    .unauthorized


        case 404:

            throw
                StudyTaskServiceError
                    .moduleNotFound


        case 408:

            throw
                StudyTaskServiceError
                    .creationUncertain


        case 500...599:

            throw
                StudyTaskServiceError
                    .creationUncertain


        default:

            throw
                StudyTaskServiceError
                    .server(
                        httpResponse
                            .statusCode
                    )
        }


        guard let task =
                try? JSONDecoder()
                    .decode(
                        StudyTask.self,
                        from: data
                    )
        else {

            throw
                StudyTaskServiceError
                    .creationUncertain
        }


        guard
            !task.id
                .trimmingCharacters(
                    in: .whitespacesAndNewlines
                )
                .isEmpty,

            !task.title
                .trimmingCharacters(
                    in: .whitespacesAndNewlines
                )
                .isEmpty

        else {

            throw
                StudyTaskServiceError
                    .creationUncertain
        }


        return task
    }
}
