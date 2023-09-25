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
        requestButton.val(`Items Selected (${selectedCount + 1}/${swapSize})`);
        requestButton.attr("disabled", "");
    }
    else if (selectedCount === swapSize - 1)
    {
        $(e).toggleClass("selected");
        placeHolders.eq(0).toggleClass("placeholder").find("img").data(("item-id"), selectedItem.data("item-id")).attr("src", selectedItem.attr("src"));
        requestButton.text(buttonText);
        requestButton.val(buttonText);
        if (requestButton.prop("tagName").toLowerCase() === "button") {
            requestButton.removeAttr("disabled");
        }
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
    $(e).find("img").attr("src", "");
    selectedItem.removeClass("selected");

    if (selectedCount == 1) {
        //swapContainer.find(".your-selection").addClass("d-none");
    }

    requestButton.text(`Items Selected (${selectedCount - 1}/${swapSize})`);
    requestButton.val(`Items Selected (${selectedCount - 1}/${swapSize})`);
    requestButton.attr("disabled", "");
}

function ToggleSwapItems(e) {
    $(e).toggleClass("selected");
    $(e).closest(".swap-container-body").find(".your-items").toggleClass("d-none");
}

function RequestDonation(e) {
    const swapContainer = $(e).prev(".swap-container");

    var swapRequestData = {
        ReceiverId: swapContainer.find(".swap-profile").data("user-id"),        // This will be stored as SenderId in database as the donater is the sender
        CollectionId: swapContainer.data("collection-id"),
        SenderUserCollectionId: swapContainer.data("sender-collection-id"),
        ReceiverUserCollectionId: swapContainer.data("receiver-collection-id"),
        StartDate: new Date().toISOString(),
        Status: "requested"
    }

    const formData = new FormData();
    Object.entries(swapRequestData).forEach(([key, value]) => {
        formData.append(key, value);
    });

    HandleFormSubmit("/Swap/ProcessSwap", "POST", formData);
}

function ConfirmDonation(e, swapId) {
    const swapContainer = $(e).parent().prev(".swap-container");

    var swapRequestData = {
        SwapId: swapId,
        SenderUserCollectionId: swapContainer.data("sender-collection-id"),
        Status: "confirmed"
    }

    const formData = new FormData();
    Object.entries(swapRequestData).forEach(([key, value]) => {
        formData.append(key, value);
    });

    HandleFormSubmit("/Swap/ProcessSwap", "POST", formData);
}

function RequestSwap(e) {
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
        SwapSize: +swapContainer.find(".swap-size").text(),
        Status: "requested"
    }

    const formData = new FormData();
    Object.entries(swapRequestData).forEach(([key, value]) => {
        formData.append(key, value);
    });

    HandleFormSubmit("/Swap/ProcessSwap", "POST", formData);
}

function AcceptSwap(e, swapId) {
    const swapContainer = $(e).parent().prev(".swap-container");
    const senderItems = swapContainer.find(".swap-item.selected > img").map(function () {
        return +$(this).data("item-id");
    }).get();

    var swapRequestData = {
        SwapId: swapId,
        SenderItems: JSON.stringify(senderItems),
        Status: "accepted"
    }

    const formData = new FormData();
    Object.entries(swapRequestData).forEach(([key, value]) => {
        formData.append(key, value);
    });

    HandleFormSubmit("/Swap/ProcessSwap", "POST", formData);
}

function ConfirmSwap(e, swapId) {
    const swapContainer = $(e).parent().prev(".swap-container");
    const requestedItems = swapContainer.find(".swap-items").eq(0).find(".swap-item > img").map(function () {
        return +$(this).data("item-id");
    }).get();

    var swapRequestData = {
        SwapId: swapId,
        RequestedItems: JSON.stringify(requestedItems),
        Status: "confirmed"
    }

    const formData = new FormData();
    Object.entries(swapRequestData).forEach(([key, value]) => {
        formData.append(key, value);
    });

    HandleFormSubmit("/Swap/ProcessSwap", "POST", formData);
}

function DeclineSwap(e, swapId) {
    var swapRequestData = {
        SwapId: swapId,
        Status: $(e).data("type") // cancel or decline
    }

    const formData = new FormData();
    Object.entries(swapRequestData).forEach(([key, value]) => {
        formData.append(key, value);
    });

    HandleFormSubmit("/Swap/ProcessSwap", "POST", formData);
}

function toggleYourItems(e) {
    const yourItems = $(e);
    const yourItemsMessage = $(".swap-yours").find("span").not(".swap-heading");
    const toggledItems = yourItems.find(".swap-featured-item").not(".swap-example");

    yourItems.toggleClass("expanded");
    toggledItems.toggleClass("d-none");
}