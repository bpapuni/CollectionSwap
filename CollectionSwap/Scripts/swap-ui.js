function SelectItem(e) {
    const swapContainer = $(e).closest(".swap-container");
    const placeHolders = swapContainer.find(".swap-item.placeholder");
    const requestButton = swapContainer.next(".submit-button").length == 0 ? swapContainer.next().find(".submit-button").eq(0) : swapContainer.next(".submit-button");
    const swapSize = swapContainer.find(".swap-size").text();
    const selectedCount = swapContainer.find(".swap-item.selected").length;
    const selectedItem = $(e).find("img");
    const buttonText = requestButton.hasClass("offer") ? "Request Swap" : requestButton.hasClass("accept") ? "Accept Swap" : "Confirm Swap";

    if ($(e).hasClass("selected")) {
        const placeholderItem = swapContainer.find(".your-selection").find(`[src='${selectedItem.attr("src")}']`).parent();
        ClearItem(placeholderItem);
    }
    else if (selectedCount < swapSize - 1)
    {
        $(e).toggleClass("selected");
        //swapContainer.find(".your-selection").removeClass("d-none");
        placeHolders.eq(0).toggleClass("placeholder").find("img").data(("item-id"), selectedItem.data("item-id")).attr("src", selectedItem.attr("src"));
        requestButton.text(`Items Selected (${selectedCount + 1}/${swapSize})`);
        requestButton.attr("disabled", "");
    }
    else if (selectedCount === swapSize - 1)
    {
        $(e).toggleClass("selected");
        placeHolders.eq(0).toggleClass("placeholder").find("img").data(("item-id"), selectedItem.data("item-id")).attr("src", selectedItem.attr("src"));
        requestButton.text(buttonText);
        requestButton.removeAttr("disabled");
    }
}

function ClearItem(e) {
    const swapContainer = $(e).closest(".swap-container");
    const requestButton = swapContainer.next(".submit-button").length == 0 ? swapContainer.next().find(".submit-button").eq(0) : swapContainer.next(".submit-button");
    const swapSize = swapContainer.find(".swap-size").text();
    const selectedCount = swapContainer.find(".swap-item.selected").length;
    const selectedItem = swapContainer.find(".selection-pool").find(`[src='${$(e).find("img").attr("src")}']`).parent();

    if ($(e).hasClass("placeholder")) {
        return;
    }

    $(e).addClass("placeholder");
    selectedItem.removeClass("selected");

    if (selectedCount == 1) {
        //swapContainer.find(".your-selection").addClass("d-none");
    }

    requestButton.text(`Items Selected (${selectedCount - 1}/${swapSize})`);
    requestButton.attr("disabled", "");
}

function ToggleSwapItems(e) {
    $(e).toggleClass("selected");
    $(e).closest(".swap-container-body").find(".your-items").toggleClass("d-none");
}

function offerSwap(e) {
    const swapContainer = $(e).prev(".swap-container");
    const senderItems = swapContainer.find(".your-items .swap-item > img").map(function () {
        return +$(this).data("item-id");
    }).get();
    const requestedItems = swapContainer.find(".swap-item.selected > img").map(function () {
        return +$(this).data("item-id");
    }).get();

    var swapRequestData = {
        ReceiverId: swapContainer.find(".swap-profile").data("user-id"),
        CollectionId: swapContainer.data("collection-id"),
        SenderUserCollectionId: swapContainer.data("sender-collection-id"),
        ReceiverUserCollectionId: swapContainer.data("receiver-collection-id"),
        SenderItems: JSON.stringify(senderItems),
        RequestedItems: JSON.stringify(requestedItems),
        StartDate: new Date().toISOString(),
        Status: "offered"
    }

    const formData = new FormData();
    Object.entries(swapRequestData).forEach(([key, value]) => {
        formData.append(key, value);
    });

    HandleFormSubmit("/Swap/ProcessSwap", "POST", formData);
}

function acceptSwap(e, swapId) {
    const swapContainer = $(e).parent().prev(".swap-container");
    const senderItems = swapContainer.find(".swap-item.selected > img").map(function () {
        return +$(this).data("item-id");
    }).get();
    const requestedItems = swapContainer.find(".receiver-items .swap-item > img").map(function () {
        return +$(this).data("item-id");
    }).get();

    var swapRequestData = {
        SwapId: swapId,
        ReceiverId: swapContainer.data("receiver-id"),
        CollectionId: swapContainer.data("collection-id"),
        SenderUserCollectionId: swapContainer.data("sender-collection-id"),
        ReceiverUserCollectionId: swapContainer.data("receiver-collection-id"),
        SenderItems: JSON.stringify(senderItems),
        RequestedItems: JSON.stringify(requestedItems),
        //StartDate: new Date().toISOString(),
        Status: "accepted"
    }

    const formData = new FormData();
    Object.entries(swapRequestData).forEach(([key, value]) => {
        formData.append(key, value);
    });

    HandleFormSubmit("/Swap/ProcessSwap", "POST", formData);
}

function confirmSwap(e, swapId) {
    const swapContainer = $(e).parent().prev(".swap-container");
    const senderItems = swapContainer.find(".swap-items").eq(1).find(".swap-item > img").map(function () {
        return +$(this).data("item-id");
    }).get();
    const requestedItems = swapContainer.find(".swap-items").eq(0).find(".swap-item > img").map(function () {
        return +$(this).data("item-id");
    }).get();

    var swapRequestData = {
        SwapId: swapId,
        ReceiverId: swapContainer.data("receiver-id"),
        CollectionId: swapContainer.data("collection-id"),
        SenderUserCollectionId: swapContainer.data("sender-collection-id"),
        ReceiverUserCollectionId: swapContainer.data("receiver-collection-id"),
        SenderItems: JSON.stringify(senderItems),
        RequestedItems: JSON.stringify(requestedItems),
        ////StartDate: new Date().toISOString(),
        Status: "confirmed"
    }

    const formData = new FormData();
    Object.entries(swapRequestData).forEach(([key, value]) => {
        formData.append(key, value);
    });

    HandleFormSubmit("/Swap/ProcessSwap", "POST", formData);
    //const swapButton = $(e);
    //const acceptedSwap = serializedAcceptedSwaps.find(swap => swap.Id === id);

    //var swapData = {
    //    Id: acceptedSwap.Id,
    //    SenderId: acceptedSwap.Sender.Id,
    //    ReceiverId: acceptedSwap.Receiver.Id,
    //    CollectionId: acceptedSwap.CollectionId,
    //    SenderUserCollectionId: acceptedSwap.SenderUserCollectionId,
    //    ReceiverUserCollectionId: acceptedSwap.ReceiverUserCollectionId,
    //    SenderItemIdsJSON: acceptedSwap.SenderItemIdsJSON,
    //    ReceiverItemIdsJSON: acceptedSwap.ReceiverItemIdsJSON,
    //    StartDate: acceptedSwap.StartDate,
    //    EndDate: new Date().toISOString(),
    //    Status: "confirmed"
    //}

    //$.ajax({
    //    url: "/Swap/ProcessSwap",
    //    type: "POST",
    //    data: swapData,
    //    dataType: "json",
    //    success: function (response) {
    //        if (response.reloadPage) {
    //            location.reload();
    //        }
    //    },
    //    error: function (xhr, status, error) {
    //    }
    //})
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