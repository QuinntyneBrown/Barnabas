import { ProblemDetails } from './problem-details';

/** A failed call, in the shape the screens need to render it. */
export class ApiError extends Error {
  constructor(
    readonly status: number,
    override readonly message: string,
    readonly fieldErrors: Readonly<Record<string, string>> = {},
  ) {
    super(message);
    this.name = 'ApiError';
  }

  static fromProblemDetails(status: number, problem: ProblemDetails | null): ApiError {
    const fieldErrors: Record<string, string> = {};

    for (const [field, messages] of Object.entries(problem?.errors ?? {})) {
      const first = messages[0];
      if (first) {
        fieldErrors[camelCase(field)] = first;
      }
    }

    return new ApiError(status, problem?.title ?? 'The request could not be completed.', fieldErrors);
  }
}

/** ASP.NET Core names fields as they appear on the command, in PascalCase. */
function camelCase(field: string): string {
  return field.length === 0 ? field : field[0].toLowerCase() + field.slice(1);
}
