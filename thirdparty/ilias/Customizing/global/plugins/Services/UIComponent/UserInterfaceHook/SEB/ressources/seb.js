function seb_init() {
	addLogin();
	editLogoLink();
	hideFooter();
	hidePermaLink();
}

function addLogin() {
	// cut logout
	var logout = $('.ilLogin a').wrapAll('<a></a>').parent().html();
	// build new html
	$('.ilLogin').html("<span class=\"sebFullname\">"+seb_object.user.firstname + " " + seb_object.user.lastname + "</span><span class=\"sebLogin\"> (" + seb_object.user.login + ")</span> >> " + logout);
}

function editLogoLink() {
	$('#il_main_logo a').attr('href',seb_object.logo.link);
}

function hideFooter() {
	$('.il_Footer').hide();
}

function hidePermaLink() {
	$('.ilPermaLink').hide();
}

window.addEventListener("load", seb_init, false);


