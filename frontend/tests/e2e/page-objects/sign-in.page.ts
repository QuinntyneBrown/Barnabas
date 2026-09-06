import { Locator, Page } from '@playwright/test';

/** Where a member asks for a sign-in link. */
export class SignInPage {
  constructor(private readonly page: Page) {}

  get emailAddress(): Locator {
    return this.page.getByLabel('Your email address');
  }

  get submit(): Locator {
    return this.page.getByRole('button', { name: 'Email me a sign-in link' });
  }

  get heading(): Locator {
    return this.page.getByRole('heading', { level: 1 });
  }

  get passwordFields(): Locator {
    return this.page.locator('input[type="password"]');
  }

  async goto(): Promise<void> {
    await this.page.goto('/sign-in');
  }

  async askForALink(emailAddress: string): Promise<void> {
    await this.emailAddress.fill(emailAddress);
    await this.submit.click();
  }
}
