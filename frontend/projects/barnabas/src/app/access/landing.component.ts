import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

/** The public front door, and where signing out leaves a member. */
@Component({
  selector: 'bar-landing',
  imports: [RouterLink],
  templateUrl: './landing.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LandingComponent {}
