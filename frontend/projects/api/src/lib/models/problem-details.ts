/**
 * The RFC 7807 payload the API returns on every failure.
 *
 * `errors` names the fields at fault and carries their messages. It never echoes the
 * submitted value — echoing is how a validation message becomes a reflection vector,
 * and the member already knows what they typed.
 */
export interface ProblemDetails {
  readonly type?: string;
  readonly title?: string;
  readonly status?: number;
  readonly detail?: string;
  readonly errors?: Readonly<Record<string, readonly string[]>>;
}
