import { ListingDetailPage } from '../page-objects/listing-detail.page';
import { EditListingPage, MyListingsPage } from '../page-objects/my-listings.page';
import { Members, SeededListings } from '../support/members';
import { expect, test } from '../support/barnabas';

// Acceptance Test
// Traces to: L2-036, L2-038, L2-039, L2-040
// Description: An owner keeps their own listings in order — correcting one, taking one off the
// board, putting it back, and deleting one behind a confirmation they can decline.

test.beforeEach(async ({ signInAs }) => {
  await signInAs(Members.marion.emailAddress);
});

// L2-036 AC3: Given a member editing their listing, when the screen loads, then it is headed as
// an edit, its fields are populated, and its action reads as saving rather than posting.
test('the edit screen is headed as an edit, filled in, and saves', async ({ page, board }) => {
  const mine = new MyListingsPage(page);
  const edit = new EditListingPage(page);

  await mine.goto();
  await mine.edit(SeededListings.ladder).click();

  await expect(edit.heading).toBeVisible();

  // Filled in with what is already there. An edit form that opened empty would invite a member
  // to retype what they wrote, and a blank field submitted is a field cleared.
  await expect(edit.title).toHaveValue(SeededListings.ladder);
  await expect(edit.description).not.toHaveValue('');

  // Saving, not posting.
  await expect(edit.save).toBeVisible();

  // The kind is not editable at all — absent, not disabled.
  await expect(edit.kind).toHaveCount(0);

  await edit.title.fill('Extending ladder, three sections, fibreglass');
  await edit.save.click();

  // Saving lands on the listing itself. Waited for rather than assumed: navigating away while
  // the save is in flight aborts it, and the board would then honestly show the old title.
  await expect(new ListingDetailPage(page).title).toHaveText(
    'Extending ladder, three sections, fibreglass',
  );

  await board.goto();

  await expect(board.placard('Extending ladder, three sections, fibreglass')).toBeVisible();
});

// L2-038 AC2: Given a member archiving a listing, when they confirm, then the archived list is
// shown containing that listing.
test('a listing taken off the board joins the put-away list', async ({ page, board }) => {
  const mine = new MyListingsPage(page);

  await mine.goto();
  await mine.takeOffTheBoard(SeededListings.ladder).click();

  await mine.confirmIn('Take this off the board?', 'Take it off').click();

  await mine.showPutAway();

  await expect(mine.row(SeededListings.ladder)).toBeVisible();

  await board.goto();

  await expect(board.placard(SeededListings.ladder)).toHaveCount(0);
});

// L2-039 AC2: Given a member viewing their archived listings, when they restore one, then it
// appears in their active listings.
test('a put-away listing can be put back on the board', async ({ page, board }) => {
  const mine = new MyListingsPage(page);

  await mine.goto();
  await mine.takeOffTheBoard(SeededListings.ladder).click();
  await mine.confirmIn('Take this off the board?', 'Take it off').click();

  await mine.showPutAway();
  await mine.putBack(SeededListings.ladder).click();

  // The screen follows it back to where it now is, rather than leaving the member on an empty tab.
  await expect(mine.onTheBoardTab).toHaveAttribute('aria-pressed', 'true');
  await expect(mine.row(SeededListings.ladder)).toBeVisible();

  await board.goto();

  await expect(board.placard(SeededListings.ladder)).toBeVisible();
});

// A listing that was closed out is archived too, and offers no way back. The rule L2-037 and
// L2-039 would otherwise disagree about, stated on the screen.
test('a closed-out listing is not offered a way back', async ({ page }) => {
  const mine = new MyListingsPage(page);

  await mine.goto();
  // The button carries the verb belonging to the kind; the dialog that follows is the same one
  // for all four, because what it asks is the same question.
  await expect(mine.closeOut(SeededListings.ladder)).toHaveText('Mark as taken');

  await mine.closeOut(SeededListings.ladder).click();
  await mine.confirmIn('Mark this as gone?', 'Mark it').click();

  await mine.showPutAway();

  await expect(mine.row(SeededListings.ladder)).toBeVisible();

  // Absent, because a returned loan has already finished. Deleting it is still offered.
  await expect(mine.putBack(SeededListings.ladder)).toHaveCount(0);
  await expect(mine.remove(SeededListings.ladder)).toBeVisible();
});

// L2-040 AC2: Given a member deleting a listing, when they choose to delete, then a confirmation
// is required before it proceeds.
// L2-040 AC3: Given the delete confirmation, when the member cancels, then the listing remains.
test('deleting is confirmed, and cancelling keeps the listing', async ({ page }) => {
  const mine = new MyListingsPage(page);

  await mine.goto();
  await mine.takeOffTheBoard(SeededListings.ladder).click();
  await mine.confirmIn('Take this off the board?', 'Take it off').click();

  await mine.showPutAway();
  await mine.remove(SeededListings.ladder).click();

  // Asked before anything happens, and told what goes with it.
  await expect(mine.dialog('Delete this for good?')).toBeVisible();
  await expect(mine.dialog('Delete this for good?')).toContainText('cannot be undone');

  await mine.cancelIn('Delete this for good?').click();

  await expect(mine.row(SeededListings.ladder)).toBeVisible();

  // And then, having meant it, it goes.
  await mine.remove(SeededListings.ladder).click();
  await mine.confirmIn('Delete this for good?', 'Delete it').click();

  await expect(mine.row(SeededListings.ladder)).toHaveCount(0);
});
