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

/**
 * What every request form has in common, and what none of them may have.
 *
 * The absent fields are exposed rather than merely unused. L2-063 requires that no request form
 * contain a payment, delivery, or deposit field, and a page object with no way to look for one
 * could not state that - the assertion would pass by having nothing to say.
 */
abstract class RequestFormPage {
  constructor(protected readonly page: Page) {}

  get message(): Locator {
    return this.page.getByLabel(/Your message to/);
  }

  /** The action that sends it, named in the words its kind uses. */
  abstract get send(): Locator;

  get payment(): Locator {
    return this.page.getByLabel(/card|payment|deposit/i);
  }

  get delivery(): Locator {
    return this.page.getByLabel(/delivery|shipping|address/i);
  }

  get returnBy(): Locator {
    return this.page.getByLabel(/back by|return/i);
  }
}

/** Asking for something being given away. */
export class RequestGivePage extends RequestFormPage {
  get heading(): Locator {
    return this.page.getByRole('heading', { name: /Ask .* for this/ });
  }

  get pickupAt(): Locator {
    return this.page.getByLabel('When could you collect it?');
  }

  get send(): Locator {
    return this.page.getByRole('button', { name: 'Request this', exact: true });
  }

  async fillAndSend(request: { message: string; pickupAt: string }): Promise<void> {
    await this.message.fill(request.message);
    await this.pickupAt.fill(request.pickupAt);
    await this.send.click();
  }
}

/** Asking to buy something. */
export class RequestSellPage extends RequestFormPage {
  get heading(): Locator {
    return this.page.getByRole('heading', { name: /Ask .* to buy this/ });
  }

  get pickupAt(): Locator {
    return this.page.getByLabel('When could you collect it?');
  }

  get send(): Locator {
    return this.page.getByRole('button', { name: 'Request to buy' });
  }

  /** The sentence that says where the money actually changes hands. */
  get paymentNote(): Locator {
    return this.page.getByText(/paid in person/i);
  }

  async fillAndSend(request: { message: string; pickupAt: string }): Promise<void> {
    await this.message.fill(request.message);
    await this.pickupAt.fill(request.pickupAt);
    await this.send.click();
  }
}

/** Asking for help, in one of the windows the offer declared. */
export class RequestHelpPage extends RequestFormPage {
  get heading(): Locator {
    return this.page.getByRole('heading', { name: /Ask .* for help/ });
  }

  /** Only the windows the listing declared, which is the whole point of the control. */
  get windows(): Locator {
    return this.page.getByRole('radio');
  }

  get windowLabels(): Locator {
    return this.page.locator('.choice-list label');
  }

  get pickupAt(): Locator {
    return this.page.getByLabel(/collect/i);
  }

  get send(): Locator {
    return this.page.getByRole('button', { name: 'Request this help' });
  }

  async fillAndSend(request: { message: string }): Promise<void> {
    await this.message.fill(request.message);
    await this.send.click();
  }
}
