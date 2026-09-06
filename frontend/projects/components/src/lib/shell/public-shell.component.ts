import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';

import { SkipLinkComponent } from './skip-link.component';

/**
 * The shell for the screens a member sees before they are signed in.
 *
 * The brand and nothing else. A navigation offering the board to somebody who cannot open it
 * would be five ways to be told to sign in, and the sign-in screen is the only thing on these
 * pages worth reaching.
 */
@Component({
  selector: 'bar-public-shell',
  imports: [RouterOutlet, RouterLink, SkipLinkComponent],
  templateUrl: './public-shell.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PublicShellComponent {}
