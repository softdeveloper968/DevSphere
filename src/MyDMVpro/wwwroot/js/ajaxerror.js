$(document).ajaxError(function (e, xhr) {
    if (xhr.status == 401)
        window.location = this.URL;
    else if (xhr.status == 403)
        alert("You do not have enough permissions to request this resource.");
});