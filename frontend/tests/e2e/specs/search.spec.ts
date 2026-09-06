import { SearchPage } from '../page-objects/search.page';
import { Members, SeededListings } from '../support/members';
import { expect, test } from '../support/barnabas';

// Acceptance Test
// Traces to: L2-048, L2-049, L2-050, L2-052
// Description: A member searches the board, narrows what comes back, and is told plainly when
// nothing matched — with the term they searched for, since there is nothing else on that screen
// to say what happened.

test.beforeEach(async ({ signInAs }) => {
  await signInAs(Members.priya.emailAddress);
});

// L2-048 AC4: Given the search screen, when a member searches for a term, then results are shown
// with the term stated.
test('a search shows what it found, and what was searched for', async ({ page }) => {
  const search = new SearchPage(page);

  await search.goto();
  await search.searchFor('ladder');

  await expect(search.results).not.toHaveCount(0);
  await expect(search.result(SeededListings.ladder)).toBeVisible();
  await expect(search.summary).toContainText('ladder');
});

// L2-049 AC2: Given a set of results, when the member narrows to one kind, then only listings of
// that kind remain and the filter is shown as applied.
test('results narrow to one kind, and the filter shows as applied', async ({ page }) => {
  const search = new SearchPage(page);

  await search.goto();

  // "a" matches both seeded listings — a ladder and a drill — so narrowing has something to do.
  await search.searchFor('a');

  await expect(search.result(SeededListings.ladder)).toBeVisible();
  await expect(search.result(SeededListings.drill)).toBeVisible();

  await search.narrowTo('For sale');

  await expect(search.result(SeededListings.drill)).toBeVisible();
  await expect(search.result(SeededListings.ladder)).toHaveCount(0);

  // Evident to a reader who cannot see which chip is darker, as well as to one who can.
  await expect(search.activeKind).toContainText('For sale');
});

// L2-050 AC2: Given the search filters, when the member opens the neighbourhood control, then
// only their congregation's neighbourhoods are offered.
test('the neighbourhood filter offers this congregation and no other', async ({ page }) => {
  const search = new SearchPage(page);

  await search.goto();

  await expect(search.neighbourhoodOptions).toContainText(['Anywhere', 'Riverdale']);

  // Parkdale belongs to St. Brigid's, and to no list Priya may choose from.
  await expect(search.neighbourhoodOptions).not.toHaveText([/Parkdale/]);
});

// L2-052 AC1: Given a search matching nothing, when results render, then the searched term is
// stated and clearing the filters is offered.
// L2-052 AC2: Given a no-results screen, when the member clears the filters, then the unfiltered
// results for that term are shown.
test('a search that finds nothing says what was searched, and offers a way back', async ({
  page,
}) => {
  const search = new SearchPage(page);

  await search.goto();
  await search.searchFor('ladder');

  await expect(search.result(SeededListings.ladder)).toBeVisible();

  // Narrowed to a kind the ladder is not, so the filters are what emptied the screen.
  await search.narrowTo('For sale');

  await expect(search.nothingFound).toContainText('ladder');
  await expect(search.clearFilters).toBeVisible();

  await search.clearFilters.click();

  // The same term, unfiltered, rather than an empty search screen.
  await expect(search.result(SeededListings.ladder)).toBeVisible();
  await expect(search.summary).toContainText('ladder');
});
