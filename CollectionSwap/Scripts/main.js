const navUl = $(".manage-container-nav ul");
const subNavLinks = navUl.find("a");
var scrollTop = $(window).scrollTop();

// Map the values of our partial views to an array
const partialNames = subNavLinks.map(function () {
    return $(this).data("nav");
}).get();

// Get the current partial views name and id if there is one
var partialName = location.pathname.split("/Manage/").pop().split("/")[0];
var partialId = location.pathname.split("/Manage/").pop().split("/")[1];

// If user has navigated to a page not within our partialNames array redirect them them to the Account partial
if (!partialNames.includes(partialName)) {
    partialName = "Swaps";
}

// Find the link index of the the currently active partial view
var selectedNavLink = subNavLinks.parent().find(`[href='/Manage/${partialName}']`)
var selectedIndex = subNavLinks.index(selectedNavLink);

// Animate the link highlight to the correct position
navUl.find("li").removeClass("selected");
navUl.find("li").eq(selectedIndex).addClass("selected");

// Display the partial view
LoadPartial(partialName, `/Manage/${partialName}`)

if (partialId) {
    const formData = new FormData();
    // We want to replace Collections with EditCollection but not in the word YourCollections so we need a regex to get an exact match
    const regex = new RegExp(`\\bCollections\\b`, 'g');
    var action = partialName.replace(regex, "EditCollection").replace("YourCollections", "UserCollection").replace("Find", "DisplaySwapMatches").replace("Swaps", "SwapsPartial");

    formData.append("id", partialId);
    HandleFormSubmit(`/Manage/${action}`, "POST", formData);
}

// On back/forward navigation update the partial name and display the corresponding view
$(window).on("popstate", function (e) {
    partialName = partialName == "Index" ? "YourSwaps" : location.pathname.split("/Manage")[1] == "" ? "YourSwaps" : location.pathname.split("/Manage/").pop().split("/")[0]
    partialId = location.pathname.split("/Manage/").pop().split("/")[1];
    LoadPartial(partialName, `/Manage/${partialName}`)
    if (partialId) {
        const formData = new FormData();
        // We want to replace Collections with EditCollection but not in the word YourCollections so we need a regex to get an exact match
        const regex = new RegExp(`\\bCollections\\b`, 'g');
        const action = partialName.replace(regex, "EditCollection").replace("YourCollections", "UserCollection").replace("Find", "DisplaySwapMatches").replace("Swaps", "SwapsPartial");
        formData.append("id", partialId);

        HandleFormSubmit(`/Manage/${action}`, "POST", formData);
    }
});

// Automatically submit the form when changing an items image
$(document).on("change", "#items input[type='file']", function (e) {
    e.preventDefault();
    $(this).closest("form").submit();
});

// Subnav click listeners
$(document).on("click", ".manage-container-nav a", function (event) {
    event.preventDefault();
    const url = $(this).attr("href").split("/Manage/").pop().split("/");
    partialName = url[0];
    partialId = url[1] == '#' ? '' : url[1];

    const historyEntry = partialId == undefined ? partialName : `${partialName}/${partialId}`;
    history.pushState({ partialName: historyEntry }, null, `/Manage/${historyEntry}`);

    LoadPartial(partialName, $(this).attr("href"));
});

// Load content anchor click listeners
$(document).on("click", ".load-content", function (e) {
    e.preventDefault();
    $("#create-user-collection-container").addClass("d-none");
    const self = $(this);
    const controller = self.attr("href").split("/")[1];
    var action = self.attr("href").split("/")[2];
    const id = self.attr("href").split("/")[3];

    const historyEntry = id == undefined ? action : `${action}/${id}`;
    history.pushState({ partialName: action }, null, `/${controller}/${historyEntry}`);

    const formData = new FormData();
    formData.append(action == "Member" ? "username" : "id", id);

    // We want to replace Collections with EditCollection but not in the word YourCollections so we need a regex to get an exact match
    const regex = new RegExp(`\\bCollections\\b`, 'g');
    action = action.replace(regex, "EditCollection").replace("YourCollections", "UserCollection").replace("Find", "DisplaySwapMatches").replace("Swaps", "SwapsPartial");

    HandleFormSubmit(`/${controller}/${action}`, "POST", formData);
});

function OpenOffer(swapId) {
    const formData = new FormData();
    formData.append("id", swapId);

    history.pushState({ partialName: "Swaps" }, null, `/Manage/Swaps/${swapId}`);
    HandleFormSubmit(`/Manage/SwapsPartial`, "POST", formData);
}

// Breadcrumb navigation button click listeners
$(document).on("click", ".navigation", function (e) {
    e.preventDefault();
    const self = $(this);
    const navTarget = $("div[data-nav]").filter(function () {
        return $(this).data("nav") === self.text();
    });
    ScrollRowBack(navTarget);
});

// Form submit listeners Handle loading form submission partial views
$(document).on("submit", ".manage-container-main form, #home-container form", function (event) {
    event.preventDefault();
    var formData = new FormData(this);
    const token = $(this).find("input[name='__RequestVerificationToken']").val();

    if (this.id == "feedback-form") {
        var rating = $(".star-button.selected").length > 0 ? $(".star-button.selected").length : null;
        var comments = [];

        $("#Feedback_Rating").val(rating);

        $(".feedback-list .selected").each(function () {
            comments.push($(this).text());
        });

        $("#Feedback_Comments").val(JSON.stringify(comments));
        formData = new FormData(this);
    }

    HandleFormSubmit($(this).attr("action"), $(this).attr("method"), formData, token);
});

// Handle loading the navigation partial views
function LoadPartial(partialName, url) {
    selectedNavLink = subNavLinks.parent().find(`[href='/Manage/${partialName.split("/")[0]}']`)
    selectedIndex = subNavLinks.index(selectedNavLink);
    navUl.find("li").removeClass("selected");
    navUl.find("li").eq(selectedIndex).addClass("selected");

    $.ajax({
        url: `${url}Partial`,
        type: "POST",
        success: function (result) {
            $('.manage-container-main').html(result.PartialView);
        },
        error: function () {
            // Handle error
        }
    });
}

function HandleFormSubmit(url, type, formData, token) {
    const headers = {};
    if (token !== null) {
        headers["X-CSRF-Token"] = token;
    }

    $.ajax({
        url: url,
        type: type,
        headers: headers,
        data: formData,
        processData: false,
        contentType: false,
        success: function (result) {
            // The returned view
            var partialView = result.PartialView;

            if (result.RefreshTargets) {
                $.each(result.RefreshTargets, function (key, target) {
                    if (`#${$(partialView).attr("id")}` === target || `.${$(partialView).attr("class")}` == target) {
                        $(`${target}`).prop("outerHTML", $(partialView).prop("outerHTML"));
                    } else {
                        const loadedContent = $(partialView).find(target);
                        if (loadedContent.length == 0) {
                            $("body").html($(partialView));
                        }
                        else {
                            $(`${target}`).prop("outerHTML", loadedContent.prop("outerHTML"));
                        }
                            
                    }
                    $(`${target}`).removeClass("d-none");
                });
            }

            if (result.ScrollTarget) {
                var offset = $(result.ScrollTarget).prop("offsetLeft");
                $(result.ScrollTarget).parent().css("scroll-snap-type", "none");

                $(document).scrollTop(0);
                $(result.ScrollTarget).parent().animate({ scrollLeft: offset }, 250, "swing", () => {
                    $(result.ScrollTarget).parent().css("scroll-snap-type", "x mandatory")
                });

                UpdateNavText();
            }

            if (result.ScrollRowBack) {
                ScrollRowBack(null);
            }

            if (result.FormResetTarget) {
                ResetHomePageSponsorForms();
                $(":input", result.FormResetTarget)
                    .not(":button, :submit, :reset, :hidden")
                    .val("")
                    .prop("checked", false)
                    .prop("selected", false);
            }

            if (result.CloseAccount) {
                $("#logoutForm").submit();
            }
        },
        error: function () {
            
        }
    });
}

// Add new user collection
function OpenCreateUserCollection(e) {
    var scrollRow = $(e).closest(".scroll-snap-row");
    scrollRow.css("scroll-snap-type", "none");
    $("#create-user-collection-container").removeClass("d-none");
    $("#user-collection-container").addClass("d-none");

    scrollRow.animate({ scrollLeft: 1100 }, 200, "swing", () => {
        $(document).scrollTop(0);
        scrollRow.css("scroll-snap-type", "x mandatory")
        UpdateNavText();
    });
}

// Page navigation back button
function ScrollRowBack(target) {
    const scrollRow = $(".scroll-snap-row");
    const pages = scrollRow.children().length - 3;  // -3 as we want to ignore the first 2 children, and we want the pages to be 0-indexed
    const currentPageIndex = pages - scrollRow.children("div.d-none").length;
    const previousPageIndex = target == null ? currentPageIndex - 1 : $("div[data-nav]").index(target);
    const offset = previousPageIndex == 0 ? 0 : 1216 * previousPageIndex;

    scrollRow.css("scroll-snap-type", "none");
    history.pushState({ partialName: partialName }, null, `/Manage/${partialName}`);

    scrollRow.animate({ scrollLeft: offset }, 250, "swing", () => {
        $(document).scrollTop(0);
        scrollRow.css("scroll-snap-type", "x mandatory");
        scrollRow.children(`div[data-nav]:gt(${previousPageIndex})`).addClass("d-none");
        UpdateNavText();
    });
}

// Plus / Minus item listener
$(document).on("click", ".counter-add, .counter-minus", function () {
    let input = $(this).parent().find("[name='quantity']");
    let itemId = input.prop("id");
    let change = $(this).hasClass("counter-add") ? 1 : -1;
    let quantity = (+input.val() + change) > 0 ? (+input.val() + change) > 99 ? 99 : +input.val() + change : 0;

    input.val(quantity);
    if (quantity == 0) {
        $(this).parent().parent().parent().prev().addClass("item-not-owned");
    }
    if (quantity == 1) {
        $(this).parent().parent().parent().prev().removeClass("item-not-owned");
    }
});

// Item number input listener
$(document).on("input", "#user-collection-container [type='number']", function () {
    let input = $(this);
    let itemId = input.prop("id");
    let quantity = +input.val()

    if (quantity < 0) {
        input.val(0);
        $(this).parent().parent().parent().prev().removeClass("item-not-owned");
    }
});

// Feedback option toggle
$(document).on("click", ".feedback-list:not(.disabled) li", function () {
    if ($(".feedback-form-container .submit-button").is(":disabled")) {
        return;
    }
    $(this).toggleClass("selected");
});

// Feedback star toggle
$(document).on("click", ".star-button", function () {
    if ($("#feedback-container > .submit-button").is(":disabled")) {
        return;
    }
    const index = $(".star-button").index(this);

    $(".star-button").removeClass("selected");

    // Star buttons are displayed rtl so we loop from clicked index to 5
    for (var i = index; i < 5; i++) {
        $(".star-button").eq(i).addClass("selected");
    }
});

// Items sent button clicked from mailing container
$(document).on("click", ".confirm-sent-items > a", function (event) {
    event.preventDefault();
    const formData = new FormData();
    formData.append("id", $(this).data("swap-id"));
    formData.append("type", "sent");

    HandleFormSubmit("/Manage/ConfirmSentReceived", "POST", formData);
});

// Items sent / received checkbox listener
$(document).on("change", ".swap-confirm", function () {
    if (this.checked) {
        var status = $(this).closest("tr").find(".capstone p").text();
        if (status != "confirmed" && status != "Items Sent") {
            this.checked = false;
        }
        else {
            // Prevent the checkbox from being ticked until after the prompt
            this.checked = false;
            Prompt(this, true);
        }
    } else {
        this.checked = true;
    }
});

// Items sent / received checkbox listener
$(document).on("change", ".block-user-container input[type=checkbox]", function () {
    $(".block-user-container").toggleClass("blocked");
    const formData = new FormData();
    formData.append("username", $(this).data("username"));
    formData.append("isBlocked", this.checked);

    HandleFormSubmit("/Manage/BlockUser", "POST", formData);
});

// Charity collection checkbox listener
$(document).on("change", ".charity-checkbox", function () {
    const formData = new FormData();
    formData.append("id", $(this).data("user-collection-id"));

    HandleFormSubmit("/Manage/ToggleCharityCollection", "POST", formData);
});

// Swap history filter listeners
$(document).on("click", ".swap-history-filters > span", function () {
    $(".swap-history-filters > span").removeClass("selected");
    $(this).addClass("selected");
    const formData = new FormData();
    formData.append("filter", $(this).data("status"));

    //HandleFormSubmit("/Manage/FilterSwaps", "POST", formData);
    HandleFormSubmit("/Manage/SwapsPartial", "POST", formData);
});

// Edit sponsor on Manage Collections page
function EditSponsor(e) {
    const form = $("#edit-sponsor-form")[0];

    const statement = $(".sponsor-statement.admin");
    if (statement.text().includes("<")) {
        statement.prop("innerHTML", statement.text());
    }

    const formData = new FormData(form);
    formData.append("statement", statement.prop("innerHTML"));

    HandleFormSubmit("/Manage/EditSponsor", "POST", formData);
}

// Edit sponsor on Manage Collections page
function DeleteSponsor(e) {
    const form = $("#edit-sponsor-form")[0];
    const formData = new FormData(form);

    HandleFormSubmit("/Manage/DeleteSponsor", "POST", formData);
}

// Edit sponsor on Home page
function EditHomeSponsors(e) {
    const editButton = $(e);
    const buttontext = editButton.text();
    const links = $(".sponsors a");
    const addSponsorForm = $("#add-sponsor-form");
    
    editButton.text(buttontext == "Edit" ? "Close" : "Edit");
    $(".add-sponsor").toggleClass("editing");
    addSponsorForm.css({ display: "flex" });

    if (buttontext == "Close") {
        ResetEditingSponsor();
    }
}

// Edit sponsor button click listener on Home page
$(document).on("click", ".add-sponsor.editing a", function (e) {
    e.preventDefault();
    const selectedImage = $(e.target);

    const addSponsorForm = $("#add-sponsor-form");
    const editSponsorForm = $("#edit-sponsor-form");
    const imageUpload = editSponsorForm.find(".sponsor-image.placeholder");
    const url = editSponsorForm.find("#Url");

    editSponsorForm.css({ display: "flex" });
    addSponsorForm.css({ display: "none" });
    imageUpload.parent().removeClass("placeholder");

    editSponsorForm.find("input#Id").val(selectedImage.data("id"))
    imageUpload[0].src = selectedImage[0].src;
    url.val(selectedImage.parent().prop("href"));
});

function ResetEditingSponsor() {
    const editSponsorForm = $("#edit-sponsor-form");
    const addSponsorForm = $("#add-sponsor-form");

    $(".add-sponsor").removeClass("editing");
    editSponsorForm[0].reset();
    addSponsorForm[0].reset();
    editSponsorForm.css({ display: "none"});
    addSponsorForm.css({ display: "none"});
}

$(document).on("change", ".add-sponsor input[type='file'], .user-collection-sponsor-container input[type='file']", function (e) {
    e.preventDefault();
    if (this.files && this.files[0]) {
        var self = $(this);
        var reader = new FileReader();

        reader.onload = function (e) {
            self.prev().find(".sponsor-image.placeholder").parent().removeClass("placeholder");
            self.prev().find(".sponsor-image.placeholder").attr('src', e.target.result);
        }

        reader.readAsDataURL(this.files[0]);
    }
});

$(document).on("reset", ".add-sponsor form", ResetHomePageSponsorForms);

function ResetHomePageSponsorForms() {
    const editSponsorForm = $("#edit-sponsor-form");
    const addSponsorForm = $("#add-sponsor-form");
    editSponsorForm.find(".sponsor-image.placeholder").prop("src", "/Content/images/scrollbar.png");
    addSponsorForm.find(".sponsor-image.placeholder").prop("src", "/Content/images/scrollbar.png");
    editSponsorForm.find("label.admin").addClass("placeholder");
    addSponsorForm.find("label.admin").addClass("placeholder");
    $("#edit-sponsor-form").css({ display: "none" });
    $("#add-sponsor-form").css({ display: "flex" });
}

function UpdateNavText() {
    const openPages = $("div[data-nav]").not(".d-none");
    const navHtml = openPages
        .map(function () {
            return `<button class="navigation">${$(this).data("nav") }</button>`;
        })
        .get()
        .join(" > ");

    if (openPages.length > 1) {
        $(".page-navigation > .header").html(navHtml).removeClass("d-none");
    }
    else {
        $(".page-navigation > .header").html(navHtml).addClass("d-none");
    }
}
$(document).on("click", "strong.faq", function () {
    const self = $(this);
    const faqRows = $("section.faq > .column");


    faqRows.find("p").slideUp();
    if (self.parent().hasClass("selected")) {
        faqRows.removeClass("selected");
        return;
    }
    faqRows.removeClass("selected");
    self.parent().addClass("selected");
    self.next().slideToggle();
});

// Register form submit handler
$(document).on("submit", "#register-form", async function (e) {
    e.preventDefault();
    const form = $(this);
    const formData = new FormData(this);
    const token = form.find("input[name='__RequestVerificationToken']").val();
    const passwordInput = form.find("#RegisterViewModel_Password");
    const password = passwordInput.val();

    const hasInvalid = await ValidatePassword(true);
    if (hasInvalid) {
        return false;
    }

    $("#register-form input[type='text'], #register-form input[type='email'], #register-form input[type='password']").prop("disabled", true);
    $("#register-form .submit-button").toggleClass("d-none");
    HandleFormSubmit($(this).attr("action"), $(this).attr("method"), formData, token);
});

// Register form password validation
$(document).on("input", "#register-form input[type='password']", ValidatePassword);

function ValidatePassword(checkForInvalid) {
    return new Promise((resolve, reject) => {
        var password = $("#RegisterViewModel_Password").val();

        $.get("/RemoteValidation/IsPasswordValid", { RegisterViewModel_Password: password }, function (data) {
            $(".password-validation span").removeClass("valid invalid");

            // Update the class of each span based on the validation results
            $.each(data, function (index, item) {
                if (item.IsValid) {
                    $(".password-validation span." + item.Rule).addClass("valid");
                } else if (checkForInvalid == true) {
                    $(".password-validation span." + item.Rule).addClass("invalid");
                }
            });

            // Check for invalid spans
            var hasInvalid = $(".password-validation span.invalid").length > 0;
            resolve(hasInvalid);
        });
    });
}

function ShowSpinner(e) {
    $(e).find("i").removeClass("d-none");
}

$(document).on("click", ".prompt-container", function (e) {
    // Guard clause if the click is on a child of the prompt container
    if (!e.target.classList.contains("prompt-container")) {
        return;
    }

    // If user has clicked outside of the prompt container hide it
    Prompt(e.target, false);
})

function Prompt(e, show) {
    const self = $(e);
    // Show the prompt
    if (show) {
        $(self).next().removeClass("d-none");
    }
    // Hide the prompt
    else {
        $(self).closest(".prompt-container").addClass("d-none");
    }
}

function CloseAccount(e) {
    Prompt(e, false);
    HandleFormSubmit("/Manage/CloseAccount", "POST", null);
}

function ConfirmSentReceived(e) {
    const checkbox = $(e).closest("td").find(".swap-confirm");
    const formData = new FormData();
    formData.append("id", $(checkbox).data("swap-id"));
    formData.append("type", $(checkbox).data("type"));

    Prompt(e, false);
    HandleFormSubmit("/Manage/ConfirmSentReceived", "POST", formData);
}

