import Foundation


// MARK: - Profile Model

struct UserProfile: Decodable {

    let userId: String
    let email: String

    let firstName: String?
    let lastName: String?

    let dateOfBirth: String?
    let gender: String?
}


// MARK: - Update Request

private struct UpdateProfileRequest: Encodable {

    let firstName: String?
    let lastName: String?
    let dateOfBirth: String?
    let gender: String?
}


// MARK: - Problem Details

private struct ProfileProblemDetails: Decodable {

    let title: String?
    let detail: String?
    let errors: [String: [String]]?
}


// MARK: - Errors

enum ProfileServiceError: LocalizedError {

    case configuration

    case unauthorized

    case notFound

    case validation(String)

    case server(Int)

    case invalidResponse

    case network

    case timeout


    var errorDescription: String? {

        switch self {

        case .configuration:

            return
                "Für diese App-Version ist noch kein Server eingerichtet."


        case .unauthorized:

            return
                "Deine Sitzung ist nicht mehr gültig. "
                + "Bitte melde dich erneut an."


        case .notFound:

            return
                "Dein Profil konnte nicht gefunden werden."


        case .validation(
            let message
        ):

            return message


        case .server(
            let status
        ):

            return
                "Die Anfrage konnte nicht abgeschlossen werden "
                + "(HTTP \(status)). Bitte versuche es erneut."


        case .invalidResponse:

            return
                "Die Antwort des Servers konnte nicht gelesen werden."


        case .network:

            return
                "Der Server ist nicht erreichbar. "
                + "Bitte prüfe deine Verbindung."


        case .timeout:

            return
                "Der Server hat nicht rechtzeitig geantwortet. "
                + "Bitte versuche es erneut."
        }
    }
}


// MARK: - Profile Service

struct ProfileService {

    // MARK: Profil laden

    func getProfile(
        accessToken: String
    ) async throws -> UserProfile {

        guard
            let baseURL =
                APIConfiguration.baseURL

        else {

            throw ProfileServiceError.configuration
        }


        let url =
            baseURL.appendingPathComponent(
                "api/profile/"
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


        let (
            data,
            response
        ) = try await perform(
            request
        )


        guard
            let httpResponse =
                response as? HTTPURLResponse

        else {

            throw ProfileServiceError.invalidResponse
        }


        switch httpResponse.statusCode {

        case 200:

            do {

                return try JSONDecoder()
                    .decode(
                        UserProfile.self,
                        from: data
                    )

            } catch {

                throw ProfileServiceError.invalidResponse
            }


        case 401:

            throw ProfileServiceError.unauthorized


        case 404:

            throw ProfileServiceError.notFound


        default:

            throw ProfileServiceError.server(
                httpResponse.statusCode
            )
        }
    }


    // MARK: Profil speichern

    func updateProfile(
        accessToken: String,
        firstName: String?,
        lastName: String?,
        dateOfBirth: Date?,
        gender: String?
    ) async throws -> UserProfile {

        guard
            let baseURL =
                APIConfiguration.baseURL

        else {

            throw ProfileServiceError.configuration
        }


        let url =
            baseURL.appendingPathComponent(
                "api/profile/"
            )


        let body =
            UpdateProfileRequest(
                firstName:
                    normalized(
                        firstName
                    ),
                lastName:
                    normalized(
                        lastName
                    ),
                dateOfBirth:
                    dateOfBirth.map {
                        Self.apiDateFormatter.string(
                            from: $0
                        )
                    },
                gender:
                    gender
            )


        var request =
            URLRequest(
                url: url
            )


        request.httpMethod =
            "PUT"

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
            "application/json",
            forHTTPHeaderField:
                "Content-Type"
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


        let (
            data,
            response
        ) = try await perform(
            request
        )


        guard
            let httpResponse =
                response as? HTTPURLResponse

        else {

            throw ProfileServiceError.invalidResponse
        }


        switch httpResponse.statusCode {

        case 200:

            do {

                return try JSONDecoder()
                    .decode(
                        UserProfile.self,
                        from: data
                    )

            } catch {

                throw ProfileServiceError.invalidResponse
            }


        case 400:

            throw ProfileServiceError.validation(
                validationMessage(
                    from: data
                )
            )


        case 401:

            throw ProfileServiceError.unauthorized


        case 404:

            throw ProfileServiceError.notFound


        default:

            throw ProfileServiceError.server(
                httpResponse.statusCode
            )
        }
    }


    // MARK: - Request

    private func perform(
        _ request: URLRequest
    ) async throws -> (
        Data,
        URLResponse
    ) {

        let configuration =
            URLSessionConfiguration.ephemeral


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

            client.finishTasksAndInvalidate()
        }


        do {

            let result =
                try await client.data(
                    for: request
                )


            try Task.checkCancellation()


            return result


        } catch let error as URLError {

            if error.code == .cancelled {

                throw CancellationError()
            }


            if error.code == .timedOut {

                throw ProfileServiceError.timeout
            }


            throw ProfileServiceError.network
        }
    }


    // MARK: - Validation Message

    private func validationMessage(
        from data: Data
    ) -> String {

        guard
            let problem =
                try? JSONDecoder()
                    .decode(
                        ProfileProblemDetails.self,
                        from: data
                    )

        else {

            return
                "Die Profildaten konnten nicht gespeichert werden."
        }


        if let errors =
            problem.errors {

            let messages =
                errors.values
                    .flatMap {
                        $0
                    }


            if !messages.isEmpty {

                return messages.joined(
                    separator: "\n"
                )
            }
        }


        if let detail =
            problem.detail,
           !detail.isEmpty {

            return detail
        }


        if let title =
            problem.title,
           !title.isEmpty {

            return title
        }


        return
            "Die Profildaten konnten nicht gespeichert werden."
    }


    // MARK: - Normalization

    private func normalized(
        _ value: String?
    ) -> String? {

        guard
            let value

        else {

            return nil
        }


        let trimmed =
            value.trimmingCharacters(
                in:
                    .whitespacesAndNewlines
            )


        return trimmed.isEmpty
            ? nil
            : trimmed
    }


    // MARK: - Date

    private static let apiDateFormatter:
        DateFormatter = {

            let formatter =
                DateFormatter()


            formatter.calendar =
                Calendar(
                    identifier:
                        .gregorian
                )


            formatter.locale =
                Locale(
                    identifier:
                        "en_US_POSIX"
                )


            formatter.timeZone =
                TimeZone(
                    secondsFromGMT:
                        0
                )


            formatter.dateFormat =
                "yyyy-MM-dd"


            return formatter
        }()
}
