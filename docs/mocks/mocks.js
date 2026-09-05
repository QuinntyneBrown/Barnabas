// Barnabas mocks: tiny progressive-enhancement script.
// Every mock must remain fully viewable and navigable with JavaScript
// disabled. This file only wires up <dialog> confirmation popups that,
// without JS, simply stay closed (their trigger buttons still work as
// plain links/buttons elsewhere on the page).
(function () {
  "use strict";

  document.querySelectorAll("[data-open-dialog]").forEach(function (trigger) {
    var dialog = document.getElementById(trigger.getAttribute("data-open-dialog"));
    if (!dialog || typeof dialog.showModal !== "function") return;
    trigger.addEventListener("click", function (event) {
      event.preventDefault();
      dialog.showModal();
    });
  });

  document.querySelectorAll("[data-close-dialog]").forEach(function (closer) {
    closer.addEventListener("click", function (event) {
      var dialog = closer.closest("dialog");
      if (dialog) {
        event.preventDefault();
        dialog.close();
      }
    });
  });
})();
