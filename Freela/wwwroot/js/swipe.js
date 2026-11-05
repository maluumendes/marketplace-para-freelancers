/* SWIPING — ARRASTAR CARTÕES */

function addSwipe(card, onLeft, onRight) {
  if (!card) return;

  let startX = 0;
  let startY = 0;
  let currentX = 0;
  let dragging = false;
  let activePointer = null;

  const threshold = 100;

  card.addEventListener("pointerdown", event => {
    if (
      event.target.closest("button") ||
      event.target.closest("input") ||
      event.target.closest("textarea") ||
      event.target.closest("select")
    ) {
      return;
    }

    if (card.classList.contains("swipe-exit-left") ||
        card.classList.contains("swipe-exit-right")) {
      return;
    }

    startX = event.clientX;
    startY = event.clientY;
    currentX = 0;
    dragging = true;
    activePointer = event.pointerId;

    card.classList.add("swiping");
    card.style.setProperty("--swipe-opacity", "0");
    card.style.transform = "translateX(0) rotate(0deg)";

    if (card.setPointerCapture) {
      card.setPointerCapture(event.pointerId);
    }
  });

  card.addEventListener("pointermove", event => {
    if (!dragging || event.pointerId !== activePointer) {
      return;
    }

    currentX = event.clientX - startX;

    const currentY = event.clientY - startY;

    // Permite a rolagem vertical normal da página.
    if (
      Math.abs(currentY) > Math.abs(currentX) * 1.2 &&
      Math.abs(currentY) > 12
    ) {
      return;
    }

    const rotation = currentX / 18;
    const opacity = Math.min(Math.abs(currentX) / threshold, 1);

    card.style.transform =
      `translateX(${currentX}px) rotate(${rotation}deg)`;

    card.style.setProperty("--swipe-opacity", opacity);

    if (currentX > 12) {
      card.dataset.swipeLabel = "INTERESSE";
      card.style.setProperty("--swipe-color", "#15965b");
    } else if (currentX < -12) {
      card.dataset.swipeLabel = "PASSAR";
      card.style.setProperty("--swipe-color", "#d23c57");
    } else {
      card.dataset.swipeLabel = "";
    }
  });

  function finishSwipe(event, cancelled = false) {
    if (!dragging) return;

    if (
      event &&
      activePointer !== null &&
      event.pointerId !== activePointer
    ) {
      return;
    }

    dragging = false;
    activePointer = null;
    card.classList.remove("swiping");

    const distance = currentX;
    currentX = 0;

    if (!cancelled && distance >= threshold) {
      card.classList.add("swipe-exit-right");
      card.style.setProperty("--swipe-opacity", "0");

      window.setTimeout(() => {
        card.classList.remove("swipe-exit-right");
        card.style.transform = "";
        card.dataset.swipeLabel = "";
        card.style.removeProperty("--swipe-opacity");

        onRight();
      }, 240);

      return;
    }

    if (!cancelled && distance <= -threshold) {
      card.classList.add("swipe-exit-left");
      card.style.setProperty("--swipe-opacity", "0");

      window.setTimeout(() => {
        card.classList.remove("swipe-exit-left");
        card.style.transform = "";
        card.dataset.swipeLabel = "";
        card.style.removeProperty("--swipe-opacity");

        onLeft();
      }, 240);

      return;
    }

    // Arraste curto: o cartão volta ao lugar.
    card.style.transform = "";
    card.dataset.swipeLabel = "";
    card.style.setProperty("--swipe-opacity", "0");
  }

  card.addEventListener("pointerup", event => {
    finishSwipe(event);
  });

  card.addEventListener("pointercancel", event => {
    finishSwipe(event, true);
  });

  card.addEventListener("lostpointercapture", () => {
    if (dragging) {
      finishSwipe(null, true);
    }
  });
}