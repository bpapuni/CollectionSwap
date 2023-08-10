//var id = window.location.href.split("Create/")[1];
//var selectedCollection = $(`[href='/UserCollections/Create/${id}']`);

//if (selectedCollection.length) {
//    selectedCollection.addClass("user-collection-selected")
//}

$(".item").on("click", function () {
    alert();
    let itemQuantity = $(this).next();
    let quantity = itemQuantity.hasClass("visually-hidden") ? 1 : 0
    let input = itemQuantity.find("[type='number']");
    let itemId = input.prop("id");

    itemQuantity.toggleClass("visually-hidden");
    input.val(quantity);
    $(this).toggleClass("item-selected");

    //UpdateUserCollection(itemId, quantity);
})

$(".counter-add, .counter-minus").on("click", function () {
    let input = $(this).parent().find("[name='quantity']");
    let itemId = input.prop("id");
    let change = $(this).hasClass("counter-add") ? 1 : -1;
    let quantity = +input.val() + change;

    input.val(quantity);
    if (quantity == 0) {
        $(this).parent().parent().prev().trigger("click");
        //UpdateUserCollection(itemId, quantity);
    }
})

$("[type='number']").on("input", function () {
    let input = $(this);
    let itemId = input.prop("id");
    let quantity = +input.val()

    if (quantity == 0) {
        $(this).parent().parent().prev().trigger("click");
        //UpdateUserCollection(itemId, quantity);
    }
})