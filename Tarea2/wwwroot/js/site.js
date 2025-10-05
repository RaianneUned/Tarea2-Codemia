// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

$(function () {
  const $overlay = $("#loginOverlay");
  const $triggers = $(".js-login-trigger");
  const $close = $("#loginOverlayClose");
  const $form = $("#loginForm");
  const $feedback = $("#loginFormFeedback");
  let $activeTrigger = $();

  const toggleScroll = (disable) => {
    $("body").toggleClass("overflow-hidden", disable);
  };

  const openOverlay = () => {
    $overlay.addClass("active").attr("aria-hidden", "false");
    if ($activeTrigger.length) {
      $activeTrigger.attr("aria-expanded", "true");
    }
    toggleScroll(true);
    setTimeout(() => $("#loginIdentifier").trigger("focus"), 150);
  };

  const closeOverlay = () => {
    $overlay.removeClass("active").attr("aria-hidden", "true");
    if ($activeTrigger.length) {
      $activeTrigger.attr("aria-expanded", "false");
      $activeTrigger = $();
    }
    toggleScroll(false);
  };

  $triggers.on("click", function (event) {
    event.preventDefault();
    const $clicked = $(this);

    if ($overlay.hasClass("active") && $activeTrigger.is($clicked)) {
      closeOverlay();
      return;
    }

    $feedback.addClass("d-none");
    $form[0].reset();
    $activeTrigger = $clicked;
    openOverlay();
  });

  $close.on("click", closeOverlay);

  $overlay.on("click", (event) => {
    if ($(event.target).is(".login-overlay, .login-overlay-backdrop")) {
      closeOverlay();
    }
  });

  $(document).on("keyup", (event) => {
    if (event.key === "Escape" && $overlay.hasClass("active")) {
      closeOverlay();
    }
  });

  $form.on("submit", (event) => {
    event.preventDefault();

    const payload = {
      identifier: $.trim($("#loginIdentifier").val()),
      password: $("#loginPassword").val()
    };

    const loginUrl = $form.data("login-url");
    $feedback.removeClass("alert-danger alert-success alert-info").addClass("d-none");

    if (!payload.identifier || !payload.password) {
      $feedback
        .text("Por favor, completa los campos requeridos.")
        .removeClass("d-none")
        .addClass("alert-danger");
      return;
    }

    const ajaxOptions = {
      url: loginUrl || "/Account/Login",
      method: "POST",
      data: payload,
      headers: {
        "X-Requested-With": "XMLHttpRequest"
      }
    };

    const $submitButton = $form.find("button[type='submit']");
    $submitButton.prop("disabled", true).text("Procesando...");

    $.ajax(ajaxOptions)
      .done((response) => {
        const message = response && response.message ? response.message : "Inicio de sesión exitoso.";
        $feedback
          .text(message)
          .removeClass("d-none")
          .addClass("alert-success");
        setTimeout(() => window.location.reload(), 800);
      })
      .fail((xhr) => {
        const message = xhr.responseJSON && xhr.responseJSON.message
          ? xhr.responseJSON.message
          : "No se pudo iniciar sesión.";
        $feedback
          .text(message)
          .removeClass("d-none")
          .addClass("alert-danger");
      })
      .always(() => {
        $submitButton.prop("disabled", false).text("Ingresar");
      });
  });
});
