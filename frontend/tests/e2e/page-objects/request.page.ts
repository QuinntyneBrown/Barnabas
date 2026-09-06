import { Locator, Page } from '@playwright/test';

/** Asking to borrow something. */
export class RequestLendPage {
  constructor(private readonly page: Page) {}

  get heading(): Locator {
    return this.page.getByRole('heading', { level: 1 });
  }

  get message(): Locator {
    return this.page.getByLabel(/Your message to/);
  }

  get pickupOn(): Locator {
    return this.page.getByLabel('When could you pick it up?');
  }

  get returnBy(): Locator {
    return this.page.getByLabel('When would you bring it back?');
  }

  get acknowledgement(): Locator {
    return this.page.getByLabel('I understand this is a loan');
  }

  get send(): Locator {
    return this.page.getByRole('button', { name: /^Send request to/ });
  }

  async fillAndSend(request: { message: string; pickupOn: string; returnBy: string }): Promise<void> {
    await this.message.fill(request.message);
    await this.pickupOn.fill(request.pickupOn);
    await this.returnBy.fill(request.returnBy);
    await this.acknowledgement.check();
    await this.send.click();
  }
}

/** The confirmation that a request is on its way. */
export class RequestSentPage {
  constructor(private readonly page: Page) {}

  /**
   * Matched on its own words rather than as "the first heading".
   *
   * The request form's own heading names the owner too, so a locator that took any level-one
   * heading would be satisfied by never having left the form - which is exactly the failure this
   * screen is meant to catch.
   */
  get heading(): Locator {
    return this.page.getByRole('heading', { name: /Your request is on its way/ });
  }

  get seeMyRequests(): Locator {
    return this.page.getByRole('link', { name: 'See my requests' });
  }

  get backToTheBoard(): Locator {
    return this.page.getByRole('link', { name: 'Back to the board' });
  }

  /** The sentence that says what Barnabas does not do. */
  get noPaymentOrDelivery(): Locator {
    return this.page.getByText(/Barnabas handles no payment, delivery, or deposit/);
  }
}

/** The confirmation that a request was accepted. */
export class RequestAcceptedPage {
  constructor(private readonly page: Page) {}

  get heading(): Locator {
    return this.page.getByRole('heading', { level: 1 });
  }

  get openTheThread(): Locator {
    return this.page.getByRole('link', { name: 'Open the message thread' });
  }
}
