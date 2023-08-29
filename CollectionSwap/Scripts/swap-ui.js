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

function SelectItem(e) {
    const swapContainer = $(e).closest(".swap-container");
    const placeHolders = swapContainer.find(".swap-item.placeholder");
    const requestButton = swapContainer.next(".submit-button");
    const swapIndex = $(".swap-container").index(swapContainer);
    const matchingSwap = serializedMatchingSwaps[swapIndex];
    const swapSize = Math.min(matchingSwap.SenderItemIds.length, matchingSwap.ReceiverItemIds.length);
    const selectedCount = swapContainer.find(".swap-item.selected").length;
    const selectedItem = $(e).find("img");

    if ($(e).hasClass("selected")) {
        const placeholderItem = swapContainer.find(".swap-items").eq(0).find(`[src='${selectedItem.attr("src")}']`).parent();
        ClearItem(placeholderItem);
    }
    else if (selectedCount < swapSize - 1)
    {
        $(e).toggleClass("selected");
        placeHolders.eq(0).toggleClass("placeholder").find("img").data(("item-id"), selectedItem.data("item-id")).attr("src", selectedItem.attr("src"));
        requestButton.text(`Items Selected (${selectedCount + 1}/${swapSize})`);
        requestButton.attr("disabled", "");
    }
    else if (selectedCount === swapSize - 1)
    {
        $(e).toggleClass("selected");
        placeHolders.eq(0).toggleClass("placeholder").find("img").data(("item-id"), selectedItem.data("item-id")).attr("src", selectedItem.attr("src"));
        requestButton.text("Request Swap");
        requestButton.removeAttr("disabled");
    }
}

function ClearItem(e) {
    const swapContainer = $(e).closest(".swap-container");
    const requestButton = swapContainer.next(".submit-button");
    const swapIndex = $(".swap-container").index(swapContainer);
    const matchingSwap = serializedMatchingSwaps[swapIndex];
    const swapSize = Math.min(matchingSwap.SenderItemIds.length, matchingSwap.ReceiverItemIds.length);
    const selectedCount = swapContainer.find(".swap-item.selected").length;
    const selectedItem = swapContainer.find(".swap-items").eq(1).find(`[src='${$(e).find("img").attr("src")}']`).parent();

    $(e).addClass("placeholder");
    selectedItem.removeClass("selected");

    requestButton.text(`Items Selected (${selectedCount - 1}/${swapSize})`);
    requestButton.attr("disabled", "");
}

function ToggleSwapItems(e) {
    $(e).toggleClass("selected");
    $(e).closest(".swap-container-body").find(".swap-items").toggleClass("d-none");
    $(e).closest(".swap-container-body").find(".header").toggleClass("d-none");
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
    const swapButton = mainContainer.closest(".swap-container-body").find(".submit-button").eq(0);

    switch (remainingSelections) {
        case 0:
            poolText.text("All selections made.");
            swapButton.removeAttr("disabled");
            break;
        default:
            poolText.text(`Select ${remainingSelections} you'd like.`);
            swapButton.attr("disabled", true);
            break;
    }
}

function offerSwap(e, swapType, id) {
    const swapContainer = $(e).prev(".swap-container");
    const requestButton = swapContainer.next(".submit-button");
    const swapIndex = $(".swap-container").index(swapContainer);

    const requestedItems = swapContainer.find(".swap-item.selected img").map(function () {
        return +$(this).data("item-id");
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
    const swapIndex = $(".submit-button").index(swapButton);
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