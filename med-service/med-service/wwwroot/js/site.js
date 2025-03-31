// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
function addEntity(url) {
    $.ajax({
        url: url,
        type: "GET",
        processData: false,
        contentType: false,
        data: null,
        async: true,
        error: function (jqxhr, textStatus, errorThrown) {
            console.error('Error', jqxhr);
            $('.modal').remove();
            $('.modal-backdrop').remove();
            bootbox.alert(jqxhr.responseText);
        }
    }).done((data) => {
        $('#modalX').empty();
        $('#modalX').append(data);
        $('.modal').modal('show');
    });
}