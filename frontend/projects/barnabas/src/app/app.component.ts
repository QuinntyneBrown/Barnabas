import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

/**
 * The application root. It holds no chrome of its own: every screen is rendered inside
 * one of two shells, chosen by the route, so the shell is a routed layout rather than
 * something this component decides.
 */
@Component({
  selector: 'bar-root',
  imports: [RouterOutlet],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
})
export class AppComponent {}
