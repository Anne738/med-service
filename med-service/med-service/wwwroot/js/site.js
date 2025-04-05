// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
function addEntity(url) {
    $.ajax({
        url: "/" + url + "/Create",
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


function showDetails(url, id) {
    $.ajax({
        url: "/" + url + "/Details/" + id,
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
        // Закриваємо модальне вікно та очищаємо фон після закриття
        $('#editModal').on('hidden.bs.modal', function () {
            $('.modal-backdrop').remove();  // Видаляємо фонове затемнення
        });
    });
}



function showEdit(url, id) {
    $.ajax({
        url: "/" + url + "/Edit/" + id,
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
        // Log the received data to see what's being returned
        console.log("Response data:", data);

        // Make sure the modal element exists
        if (!$('#modalX').length) {
            $('body').append('<div class="modal fade" id="modalX" tabindex="-1" aria-hidden="true"></div>');
        }

        // Clear and append the new content
        $('#modalX').empty();
        $('#modalX').html(data);

        // Initialize and show the modal after content is loaded
        var modal = new bootstrap.Modal(document.getElementById('modalX'));
        modal.show();
    });
}


function confirmDelete(url, id) {
    $.ajax({
        url: "/" + url + "/Delete/" + id,
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
        // Убедимся, что модальное окно существует
        if (!$('#modalX').length) {
            $('body').append('<div class="modal fade" id="modalX" tabindex="-1" aria-hidden="true"></div>');
        }

        // Очищаем и добавляем новое содержимое
        $('#modalX').empty();
        $('#modalX').html(data);

        // Инициализируем и показываем модальное окно
        var modal = new bootstrap.Modal(document.getElementById('modalX'));
        modal.show();
    });
}

//function openBookModal(doctorId, selectedDay, hour) {
//    console.log('openBookModal called', doctorId, selectedDay, hour);  // Логування аргументів

//    $.ajax({
//        url: "/Timetables/Book?day=" + selectedDay + "&hour=" + hour + "&minute=0&doctorId=" + doctorId,
//        type: "GET",
//        processData: false,
//        contentType: false,
//        data: null,
//        async: true,
//        success: function (data) {
//            console.log("Response data:", data);

//            // Перевірка, чи існує модальне вікно
//            if (!$('#bookAppointmentModal').length) {
//                // Якщо модального вікна ще немає, створимо нове
//                console.log('Creating a new modal');
//                $('body').append('<div class="modal fade" id="bookAppointmentModal" tabindex="-1" aria-labelledby="bookAppointmentModalLabel" aria-hidden="true"></div>');
//            }

//            // Очистити попередній вміст модалки
//            $('#bookAppointmentModal').empty();

//            // Вставити новий HTML-контент у модальне вікно
//            $('#bookAppointmentModal').html(data);

//            // Ініціалізувати модальне вікно та показати його
//            var modal = new bootstrap.Modal(document.getElementById('bookAppointmentModal'));
//            modal.show();
//            console.log('Modal shown');
//        },
//        error: function (jqxhr, textStatus, errorThrown) {
//            console.error('Error loading modal content', jqxhr);
//            alert("Помилка: " + jqxhr.responseText); // Простий варіант
//        }
//    });
//}


function openBookModal(doctorId, selectedDay, hour) {
    console.log('openBookModal called', doctorId, selectedDay, hour);  // Логирование аргументов

    $.ajax({
        url: "/Timetables/Book?day=" + selectedDay + "&hour=" + hour + "&minute=0&doctorId=" + doctorId,
        type: "GET",
        processData: false,
        contentType: false,
        data: null,
        async: true,
        error: function (jqxhr, textStatus, errorThrown) {
            console.error('Error loading modal content', jqxhr);
            bootbox.alert("Помилка: " + jqxhr.responseText); // Простое оповещение об ошибке
            $('.modal').remove();
            $('.modal-backdrop').remove();
        },
        success: function (data) {
            // Проверяем, есть ли уже модальное окно с таким ID
            if (!$('#modalX').length) {
                // Если модального окна нет, создаём его
                console.log('Creating a new modal');
                $('body').append('<div class="modal fade" id="modalX" tabindex="-1" aria-labelledby="modalXLabel" aria-hidden="true"></div>');
            }

            // Очищаем содержимое модального окна перед вставкой нового контента
            $('#modalX').empty();

            // Вставляем новый HTML контент в модальное окно
            $('#modalX').html(data);

            // Инициализируем и показываем модальное окно
            var modal = new bootstrap.Modal(document.getElementById('modalX'));
            modal.show();
            console.log('Modal shown');
        }
    });
}
