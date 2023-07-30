const id = window.location.href.split("UserCollection/")[1];
const selectedCollection = $(`[href='/Swap/UserCollection/${id}']`);

if (selectedCollection.length) {
    selectedCollection.addClass("user-collection-selected")
}

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
    const successMessageElement = $(".text-success").text(reloadPageMessage);
    //$("#successMessageContainer").empty().append(successMessageElement).show();
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
            const src = self.find("img").attr("src");
            poolContainer.find(`[src="${src}"]`).parent().show();
            updatedPoolText(mainContainer, poolContainer);

        }
        poolContainer.toggleClass("highlight", () => {
            setTimeout(() => {
                poolContainer.toggleClass("highlight");
            }, 200);
        });
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
    const swapButton = mainContainer.find(".create-swap");

    switch (remainingSelections) {
        case 1:
            poolText.text(`Select ${remainingSelections} item you'd like.`);
            swapButton.addClass("visually-hidden");
            break;
        case 0:
            poolText.text("All selections made.");
            swapButton.removeClass("visually-hidden");
            swapButton.toggleClass("highlight", () => {
                setTimeout(() => {
                    swapButton.toggleClass("highlight");
                }, 200);
            });
            break;
        default:
            poolText.text(`Select ${remainingSelections} items you'd like.`);
            swapButton.addClass("visually-hidden");
            break;
    }
}

function offerSwap(e) {
    const swapButton = $(e);
    const swapIndex = $(".create-swap").index(swapButton);
    const matchingSwaps = serializedMatchingSwaps;
    const offeredSwapItems = $(".swap-featured-item.selected img").map(function () {
        return +$(this).attr("alt");
    }).get();
    const selectedCollection = serializedSelectedCollection;

    if (selectedCollection != null) {

        var swapData = {
            SenderId: matchingSwaps[swapIndex].SenderId,
            ReceiverId: matchingSwaps[swapIndex].ReceiverId,
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
            url: "/Swap/HandleSwap",
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

function acceptSwap(e) {
    const swapButton = $(e);
    const swapIndex = $(".create-swap").index(swapButton);
    const offeredSwaps = serializedOfferedSwaps;
    const offeredSwapItems = $(".swap-featured-item.selected img").map(function () {
        return +$(this).attr("alt");
    }).get();

    var swapData = {
        Id: offeredSwaps[swapIndex].Id,
        SenderId: offeredSwaps[swapIndex].SenderId,
        ReceiverId: offeredSwaps[swapIndex].ReceiverId,
        CollectionId: offeredSwaps[swapIndex].CollectionId,
        SenderUserCollectionId: offeredSwaps[swapIndex].SenderUserCollectionId,
        ReceiverUserCollectionId: offeredSwaps[swapIndex].ReceiverUserCollectionId,
        SenderItemIdsJSON: JSON.stringify(offeredSwapItems),
        ReceiverItemIdsJSON: offeredSwaps[swapIndex].ReceiverItemIdsJSON,
        StartDate: offeredSwaps[swapIndex].StartDate,
        EndDate: null,
        Status: "accepted"
    }

    console.log(swapData);

    $.ajax({
        url: "/Swap/HandleSwap",
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