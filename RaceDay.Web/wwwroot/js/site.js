// RaceDay - small progressive-enhancement helpers. The site works without JavaScript;
// this only adds convenience (confirmation prompts, image checks and previews).
(function () {
  'use strict';

  // 1) Confirmation prompts: <form data-confirm="Are you sure?">
  document.addEventListener('submit', function (event) {
    var form = event.target;
    var message = form.getAttribute && form.getAttribute('data-confirm');
    if (message && !window.confirm(message)) {
      event.preventDefault();
    }
  });

  // 2) Prevent double submissions: disable the submit button once a valid form is sent.
  document.addEventListener('submit', function (event) {
    if (event.defaultPrevented) return;
    var form = event.target;
    var buttons = form.querySelectorAll ? form.querySelectorAll('button[type="submit"][data-once]') : [];
    buttons.forEach(function (button) {
      setTimeout(function () { button.disabled = true; }, 0);
    });
  });

  // 3) Image inputs: <input type="file" data-image-input data-preview="#previewId">
  //    Checks the type and size (max 5 MB) before upload and shows a preview.
  var MAX_BYTES = 5 * 1024 * 1024;
  var ALLOWED = ['image/jpeg', 'image/png', 'image/webp'];

  document.querySelectorAll('input[type="file"][data-image-input]').forEach(function (input) {
    input.addEventListener('change', function () {
      var file = input.files && input.files[0];
      var preview = input.getAttribute('data-preview') ? document.querySelector(input.getAttribute('data-preview')) : null;
      var feedback = input.parentElement.querySelector('.rd-file-feedback');

      if (feedback) feedback.textContent = '';
      if (preview) { preview.style.display = 'none'; preview.removeAttribute('src'); }
      if (!file) return;

      var problem = null;
      if (ALLOWED.indexOf(file.type) === -1) problem = 'Please choose a JPG, PNG or WEBP image.';
      else if (file.size > MAX_BYTES) problem = 'That image is larger than 5 MB. Please choose a smaller one.';

      if (problem) {
        input.value = '';
        if (feedback) feedback.textContent = problem; else window.alert(problem);
        return;
      }

      if (preview) {
        preview.src = URL.createObjectURL(file);
        preview.style.display = 'block';
      }
    });
  });
})();
