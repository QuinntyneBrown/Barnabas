import { HttpContextToken } from '@angular/common/http';

/**
 * Marks a request the refresh interceptor must leave alone.
 *
 * Renewal is itself an HTTP call, so retrying a 401 from it would call it again. Sign-in has no
 * session to renew at all. Both say so with this rather than the interceptor holding a list of
 * URLs it has to be kept in step with.
 */
export const SKIP_REFRESH = new HttpContextToken<boolean>(() => false);
