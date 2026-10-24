import { registerLocaleData } from '@angular/common';
import localeIt from '@angular/common/locales/it';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  ApplicationConfig,
  DEFAULT_CURRENCY_CODE,
  LOCALE_ID,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { routes } from './app.routes';
import { sessioneScadutaInterceptor } from './core/accesso';

registerLocaleData(localeIt);

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([sessioneScadutaInterceptor])),
    { provide: LOCALE_ID, useValue: 'it-IT' },
    { provide: DEFAULT_CURRENCY_CODE, useValue: 'EUR' },
  ],
};
