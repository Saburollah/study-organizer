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

// MARK: - Aufgabe bearbeiten

struct UpdateStudyTaskRequest: Encodable {

    let title: String

    let description: String?

    let dueDateUtc: String
}

// MARK: - Aufgabenstatus

enum StudyTaskStatus: String, Encodable {

    case open = "Open"
    case completed = "Completed"
}


struct UpdateStudyTaskStatusRequest: Encodable {

    let status: StudyTaskStatus
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
    
    case invalidTaskId
    
    case statusRejected(String)
    
    case statusUpdateUncertain
    
    case updateRejected(String)
    
    case updateConflict(String)
    
    case updateUncertain
    
    case deleteConflict(String)
    
    case deletionUncertain


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
        case .invalidTaskId:

            return
                "Die ausgewählte Aufgabe ist ungültig."


        case .statusRejected(let message):

            return message


        case .statusUpdateUncertain:

            return
                "Die Statusänderung konnte nicht bestätigt werden. "
                + "Bitte aktualisiere die Aufgabenliste."
            
        case .updateRejected(let message):

            return message


        case .updateConflict(let message):

            return message


        case .updateUncertain:

            return
                "Die Änderung der Aufgabe konnte nicht bestätigt werden. "
                + "Bitte aktualisiere die Aufgabenliste."
            
        case .deleteConflict(let message):

            return message


        case .deletionUncertain:

            return
                "Das Löschen der Aufgabe konnte nicht bestätigt werden. "
                + "Bitte aktualisiere die Aufgabenliste."
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

// MARK: - Aufgabenstatus ändern

extension StudyTaskService {

    func updateStatus(
        moduleId: String,
        taskId: String,
        status: StudyTaskStatus,
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


        guard let requestedTaskId =
                UUID(
                    uuidString: taskId
                )
        else {

            throw
                StudyTaskServiceError
                    .invalidTaskId
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
                requestedTaskId.uuidString
            )
            .appendingPathComponent(
                "status"
            )


        let body =
            UpdateStudyTaskStatusRequest(
                status: status
            )


        var request =
            URLRequest(
                url: url
            )


        request.httpMethod =
            "PATCH"


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


            if error.code
                == .timedOut {

                throw
                    StudyTaskServiceError
                        .statusUpdateUncertain
            }


            throw
                StudyTaskServiceError
                    .statusUpdateUncertain
        }


        try Task
            .checkCancellation()


        guard let httpResponse =
                response
                    as? HTTPURLResponse
        else {

            throw
                StudyTaskServiceError
                    .statusUpdateUncertain
        }


        switch httpResponse.statusCode {

        case 200:

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
                    .statusRejected(
                        problem?.message
                        ?? "Der Aufgabenstatus konnte nicht geändert werden."
                    )


        case 401:

            throw
                StudyTaskServiceError
                    .unauthorized


        case 404:

            throw
                StudyTaskServiceError
                    .statusRejected(
                        "Die Aufgabe oder das Lernmodul ist nicht mehr verfügbar."
                    )


        case 408:

            throw
                StudyTaskServiceError
                    .statusUpdateUncertain


        case 500...599:

            throw
                StudyTaskServiceError
                    .statusUpdateUncertain


        default:

            throw
                StudyTaskServiceError
                    .server(
                        httpResponse.statusCode
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
                    .statusUpdateUncertain
        }


        return task
    }
}

// MARK: - Aufgabe bearbeiten

extension StudyTaskService {

    func update(
        moduleId: String,
        taskId: String,
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


        guard let requestedTaskId =
                UUID(
                    uuidString: taskId
                )
        else {

            throw
                StudyTaskServiceError
                    .invalidTaskId
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
                requestedTaskId.uuidString
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
            UpdateStudyTaskRequest(
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
            "PUT"


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

            if error.code == .cancelled {

                throw CancellationError()
            }


            if error.code == .timedOut {

                throw
                    StudyTaskServiceError
                        .updateUncertain
            }


            throw
                StudyTaskServiceError
                    .updateUncertain
        }


        try Task
            .checkCancellation()


        guard let httpResponse =
                response
                    as? HTTPURLResponse
        else {

            throw
                StudyTaskServiceError
                    .updateUncertain
        }


        switch httpResponse.statusCode {

        case 200:

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
                    .updateRejected(
                        problem?.message
                        ?? "Die Aufgabe konnte nicht gespeichert werden."
                    )


        case 401:

            throw
                StudyTaskServiceError
                    .unauthorized


        case 404:

            throw
                StudyTaskServiceError
                    .updateRejected(
                        "Die Aufgabe oder das Lernmodul ist nicht mehr verfügbar."
                    )


        case 409:

            let problem =
                try? JSONDecoder()
                    .decode(
                        StudyTaskProblem.self,
                        from: data
                    )


            throw
                StudyTaskServiceError
                    .updateConflict(
                        problem?.message
                        ?? "Die Aufgabe wurde inzwischen geändert. Bitte aktualisiere die Aufgabenliste."
                    )


        case 408:

            throw
                StudyTaskServiceError
                    .updateUncertain


        case 500...599:

            throw
                StudyTaskServiceError
                    .updateUncertain


        default:

            throw
                StudyTaskServiceError
                    .server(
                        httpResponse.statusCode
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
                    .updateUncertain
        }


        return task
    }
}

// MARK: - Aufgabe löschen

extension StudyTaskService {

    func delete(
        moduleId: String,
        taskId: String,
        accessToken: String
    ) async throws {

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


        guard let requestedTaskId =
                UUID(
                    uuidString: taskId
                )
        else {

            throw
                StudyTaskServiceError
                    .invalidTaskId
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
                requestedTaskId.uuidString
            )


        var request =
            URLRequest(
                url: url
            )


        request.httpMethod =
            "DELETE"


        request.timeoutInterval =
            20


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

            if error.code == .cancelled {

                throw
                    CancellationError()
            }


            throw
                StudyTaskServiceError
                    .deletionUncertain
        }


        try Task
            .checkCancellation()


        guard let httpResponse =
                response
                    as? HTTPURLResponse
        else {

            throw
                StudyTaskServiceError
                    .deletionUncertain
        }


        switch httpResponse.statusCode {

        case 204:

            return


        case 401:

            throw
                StudyTaskServiceError
                    .unauthorized


        case 404:

            throw
                StudyTaskServiceError
                    .server(
                        404
                    )


        case 409:

            let problem =
                try? JSONDecoder()
                    .decode(
                        StudyTaskProblem.self,
                        from: data
                    )


            throw
                StudyTaskServiceError
                    .deleteConflict(
                        problem?.message
                        ?? "Die Aufgabe kann derzeit nicht gelöscht werden."
                    )


        case 408:

            throw
                StudyTaskServiceError
                    .deletionUncertain


        case 500...599:

            throw
                StudyTaskServiceError
                    .deletionUncertain


        default:

            throw
                StudyTaskServiceError
                    .server(
                        httpResponse.statusCode
                    )
        }
    }
}
