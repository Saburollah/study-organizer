import Foundation


// MARK: - Request

private struct ChangePasswordRequest: Encodable {

    let currentPassword: String
    let newPassword: String
}


// MARK: - Problem Details

private struct PasswordProblemDetails: Decodable {

    let title: String?
    let detail: String?
    let errors: [String: [String]]?
}


// MARK: - Errors

enum PasswordServiceError: LocalizedError {

    case configuration

    case unauthorized

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


// MARK: - Password Service

struct PasswordService {

    func changePassword(
        accessToken: String,
        currentPassword: String,
        newPassword: String
    ) async throws {

        guard
            let baseURL =
                APIConfiguration.baseURL

        else {

            throw PasswordServiceError.configuration
        }


        let url =
            baseURL.appendingPathComponent(
                "api/auth/password"
            )


        let body =
            ChangePasswordRequest(
                currentPassword:
                    currentPassword,
                newPassword:
                    newPassword
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


        do {

            request.httpBody =
                try JSONEncoder()
                    .encode(
                        body
                    )

        } catch {

            throw PasswordServiceError.invalidResponse
        }


        // Keine dauerhafte Speicherung von
        // Cookies oder Antworten.
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


        let data: Data

        let response: URLResponse


        do {

            (
                data,
                response
            ) = try await client.data(
                for: request
            )


        } catch let error as URLError {

            if error.code == .cancelled {

                throw CancellationError()
            }


            if error.code == .timedOut {

                throw PasswordServiceError.timeout
            }


            throw PasswordServiceError.network
        }


        try Task.checkCancellation()


        guard
            let httpResponse =
                response as? HTTPURLResponse

        else {

            throw PasswordServiceError.invalidResponse
        }


        switch httpResponse.statusCode {

        case 204:

            return


        case 400:

            throw PasswordServiceError.validation(
                validationMessage(
                    from: data
                )
            )


        case 401:

            throw PasswordServiceError.unauthorized


        default:

            throw PasswordServiceError.server(
                httpResponse.statusCode
            )
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
                        PasswordProblemDetails.self,
                        from: data
                    )

        else {

            return
                "Das Passwort konnte nicht geändert werden."
        }


        if let errors =
            problem.errors {

            // Backend liefert beim Passwortwechsel
            // Fehler normalerweise unter "password".
            if let passwordErrors =
                errors["password"],
               !passwordErrors.isEmpty {

                return passwordErrors.joined(
                    separator: "\n"
                )
            }


            let allMessages =
                errors.values
                    .flatMap {
                        $0
                    }


            if !allMessages.isEmpty {

                return allMessages.joined(
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
            "Das Passwort konnte nicht geändert werden."
    }
}
