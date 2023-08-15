function getAndClearCookie(name) {
    const decodedCookie = decodeURIComponent(document.cookie);
    const cookieParts = decodedCookie.split(';');
    for (let i = 0; i < cookieParts.length; i++) {
        let cookiePart = cookieParts[i].trim();
        if (cookiePart.startsWith(name + '=')) {
            const value = cookiePart.substring(name.length + 1, cookiePart.length);
            const expirationDate = new Date();
            expirationDate.setFullYear(1970);
            document.cookie = name + '=; expires=' + expirationDate.toUTCString() + '; path=/;';
            return value;
        }
    }
    return '';
}

// Get the success message from the cookie after page reload and display it
const reloadPageMessage = getAndClearCookie('swapSuccessMessage');
if (reloadPageMessage) {
    $(".text-success").text(reloadPageMessage);
}

function selectItem(e) {
    const self = $(e);
    const mainContainer = self.closest(".swap-container-main");
    const poolContainer = mainContainer.next(".swap-pool-container");
    const itemExists = self.hasClass("selected");

    self.removeClass("selected");
    self.toggleClass("selecting");
    mainContainer.toggleClass("dim");

    if (mainContainer.hasClass("dim")) {
        if (itemExists) {
            const src = self.find("img").attr("src").split('?')[0];
            poolContainer.find(`[src="${src}"]`).parent().show();
            updatedPoolText(mainContainer, poolContainer);

        }
        $(".swap-pool-item").toggleClass("highlight");
        setTimeout(() => {
            $(".swap-pool-item").toggleClass("highlight");
        }, 200);
    }
}

function selectPoolItem(e) {
    const poolItem = $(e);
    const poolContainer = poolItem.closest(".swap-pool-container");
    const mainContainer = poolContainer.prev(".swap-container-main");
    const featuredItem = mainContainer.find(".swap-featured-item.selecting");

    featuredItem.toggleClass("selecting");

    if (mainContainer.hasClass("dim")) {
        const newSrc = poolItem.find("img").attr("src");
        const newAlt = poolItem.find("img").attr("alt");

        mainContainer.removeClass("dim");
        featuredItem.addClass("selected");
        featuredItem.find("img").attr("src", newSrc);
        featuredItem.find("img").attr("alt", newAlt);
        poolItem.hide();
    }

    updatedPoolText(mainContainer, poolContainer);
}

function updatedPoolText(mainContainer, poolContainer) {
    const swapSize = mainContainer.closest(".swap-container").find(".swap-size").text();
    const itemsSelected = mainContainer.find(".swap-featured-item.selected").length;
    const remainingSelections = swapSize - itemsSelected;
    const poolText = poolContainer.find("span").eq(1);
    const swapButton = mainContainer.closest(".swap-container-body").find(".accept-swap");

    switch (remainingSelections) {
        case 0:
            poolText.text("All selections made.");
            swapButton.removeClass("visually-hidden");
            break;
        default:
            poolText.text(`Select ${remainingSelections} you'd like.`);
            swapButton.addClass("visually-hidden");
            break;
    }
}

function offerSwap(e, swapType, id) {
    const swapButton = $(e);
    const swapIndex = $(".accept-swap").index(swapButton);
    const matchingSwaps = serializedMatchingSwaps;
    const offeredSwapItems = $(".swap-featured-item.selected img").map(function () {
        return +$(this).attr("alt");
    }).get();
    const selectedCollection = serializedSelectedCollection;

    if (selectedCollection != null) {
        var swapData = {
            SenderId: matchingSwaps[swapIndex].Sender.Id,
            ReceiverId: matchingSwaps[swapIndex].Receiver.Id,
            CollectionId: matchingSwaps[swapIndex].CollectionId,
            SenderUserCollectionId: selectedCollection.Id,
            ReceiverUserCollectionId: matchingSwaps[swapIndex].UserCollectionId,
            SenderItemIdsJSON: JSON.stringify(matchingSwaps[swapIndex].ReceiverItemIds),
            ReceiverItemIdsJSON: JSON.stringify(offeredSwapItems),
            StartDate: new Date().toISOString(),
            EndDate: null,
            Status: "offered"
        }

        $.ajax({
            url: "/Swap/ProcessSwap",
            type: "POST",
            data: swapData,
            dataType: "json",
            success: function (response) {
                if (response.reloadPage) {
                    location.reload();
                }
            },
            error: function (xhr, status, error) {
            }
        })
    }
}

function acceptSwap(e, swapType, id) {
    const swapButton = $(e);
    const swapIndex = $(".accept-swap").index(swapButton);
    const offeredSwap = serializedOfferedSwaps.find(swap => swap.Id === id);
    const offeredSwapItems = $(".swap-container-main").eq(swapIndex).find(".swap-featured-item.selected img").map(function () {
        return +$(this).attr("alt");
    }).get();

    var swapData = {
        Id: offeredSwap.Id,
        SenderId: offeredSwap.Sender.Id,
        ReceiverId: offeredSwap.Receiver.Id,
        CollectionId: offeredSwap.CollectionId,
        SenderUserCollectionId: offeredSwap.SenderUserCollectionId,
        ReceiverUserCollectionId: offeredSwap.ReceiverUserCollectionId,
        SenderItemIdsJSON: JSON.stringify(offeredSwapItems),
        ReceiverItemIdsJSON: offeredSwap.ReceiverItemIdsJSON,
        StartDate: offeredSwap.StartDate,
        EndDate: null,
        Status: "accepted"
    }

    $.ajax({
        url: "/Swap/ProcessSwap",
        type: "POST",
        data: swapData,
        dataType: "json",
        success: function (response) {
            if (response.reloadPage) {
                location.reload();
            }
        },
        error: function (xhr, status, error) {
        }
    })
}

function confirmSwap(e, swapType, id) {
    const swapButton = $(e);
    const acceptedSwap = serializedAcceptedSwaps.find(swap => swap.Id === id);

    var swapData = {
        Id: acceptedSwap.Id,
        SenderId: acceptedSwap.Sender.Id,
        ReceiverId: acceptedSwap.Receiver.Id,
        CollectionId: acceptedSwap.CollectionId,
        SenderUserCollectionId: acceptedSwap.SenderUserCollectionId,
        ReceiverUserCollectionId: acceptedSwap.ReceiverUserCollectionId,
        SenderItemIdsJSON: acceptedSwap.SenderItemIdsJSON,
        ReceiverItemIdsJSON: acceptedSwap.ReceiverItemIdsJSON,
        StartDate: acceptedSwap.StartDate,
        EndDate: new Date().toISOString(),
        Status: "confirmed"
    }

    $.ajax({
        url: "/Swap/ProcessSwap",
        type: "POST",
        data: swapData,
        dataType: "json",
        success: function (response) {
            if (response.reloadPage) {
                location.reload();
            }
        },
        error: function (xhr, status, error) {
        }
    })
}

function declineSwap(e, swapType, id) {
    const swapButton = $(e);
    const swapIndex = $(".decline-swap").index(swapButton);
    const declinedSwap = swapType == "offered" ? serializedOfferedSwaps.find(swap => swap.Id === id) : serializedAcceptedSwaps.find(swap => swap.Id === id);

    var swapData = {
        Id: declinedSwap.Id,
        SenderId: declinedSwap.Sender.Id,
        ReceiverId: declinedSwap.Receiver.Id,
        CollectionId: declinedSwap.CollectionId,
        SenderUserCollectionId: declinedSwap.SenderUserCollectionId,
        ReceiverUserCollectionId: declinedSwap.ReceiverUserCollectionId,
        SenderItemIdsJSON: declinedSwap.SenderItemIdsJSON,
        ReceiverItemIdsJSON: declinedSwap.ReceiverItemIdsJSON,
        StartDate: declinedSwap.StartDate,
        EndDate: null,
        Status: "declined"
    }

    $.ajax({
        url: "/Swap/ProcessSwap",
        type: "POST",
        data: swapData,
        dataType: "json",
        success: function (response) {
            if (response.reloadPage) {
                location.reload();
            }
        },
        error: function (xhr, status, error) {
        }
    })
}

function toggleYourItems(e) {
    const yourItems = $(e);
    const yourItemsMessage = $(".swap-yours").find("span").not(".swap-heading");
    const toggledItems = yourItems.find(".swap-featured-item").not(".swap-example");

    yourItems.toggleClass("expanded");
    toggledItems.toggleClass("d-none");
    //const message = yourItems.hasClass("expanded") ?
    //    yourItemsMessage.text().replace("duplicates", "following duplicates") :
    //    yourItemsMessage.text().replace("following duplicates", "duplicates");
    //yourItemsMessage.text(message);
}