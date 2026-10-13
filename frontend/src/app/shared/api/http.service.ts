import { HttpClient, HttpContext, HttpContextToken, HttpParams } from '@angular/common/http'
import { Injectable } from '@angular/core'
import { firstValueFrom, Observable, Subject } from 'rxjs'
import { defaultIfEmpty, takeUntil } from 'rxjs/operators'

export const IS_LOADING_ENABLED = new HttpContextToken<boolean>(() => true)
export const IS_ERROR_HANDLING_ENABLED = new HttpContextToken<boolean>(() => true)

export interface ApiHttpOptions {
  skipLoading?: boolean
  skipErrorHandling?: boolean
  cancellationToken?: CancellationToken
}

export class CancellationToken {
  private subject = new Subject<void>()

  public notifier(): Observable<void> {
    return this.subject.asObservable()
  }

  public cancel(): void {
    this.subject.next()
    this.subject.complete()
  }
}

@Injectable({ providedIn: 'root' })
export class ApiHttpService {
  private readonly baseUrl = '/api'

  constructor(public readonly http: HttpClient) {}

  public get<T>(path: string, params?: Record<string, unknown>, options?: ApiHttpOptions): Promise<T> {
    const cancellationToken = options?.cancellationToken ?? new CancellationToken()
    return firstValueFrom(
      this.http
        .get<T>(`${this.baseUrl}/${path}`, {
          params: this.toHttpParams(params),
          context: this.getHttpContext(options),
        })
        .pipe(takeUntil(cancellationToken.notifier()), defaultIfEmpty(null as T)),
    )
  }

  public post<T>(path: string, body?: unknown, options?: ApiHttpOptions): Promise<T> {
    return firstValueFrom(
      this.http.post<T>(`${this.baseUrl}/${path}`, body, { context: this.getHttpContext(options) }),
    )
  }

  public put<T>(path: string, body?: unknown, options?: ApiHttpOptions): Promise<T> {
    return firstValueFrom(
      this.http.put<T>(`${this.baseUrl}/${path}`, body, { context: this.getHttpContext(options) }),
    )
  }

  public delete<T>(path: string, params?: Record<string, unknown>, options?: ApiHttpOptions): Promise<T> {
    return firstValueFrom(
      this.http.delete<T>(`${this.baseUrl}/${path}`, {
        params: this.toHttpParams(params),
        context: this.getHttpContext(options),
      }),
    )
  }

  private toHttpParams(params?: Record<string, unknown>): HttpParams {
    let httpParams = new HttpParams()
    if (!params) {
      return httpParams
    }

    for (const [key, value] of Object.entries(params)) {
      if (value === undefined || value === null) {
        continue
      }
      httpParams = httpParams.set(key, String(value))
    }

    return httpParams
  }

  private getHttpContext(options: ApiHttpOptions | undefined): HttpContext {
    const context = new HttpContext()
    if (options?.skipLoading) {
      context.set(IS_LOADING_ENABLED, false)
    }
    if (options?.skipErrorHandling) {
      context.set(IS_ERROR_HANDLING_ENABLED, false)
    }
    return context
  }
}
