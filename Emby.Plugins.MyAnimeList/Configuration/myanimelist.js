define(['loading', 'emby-input', 'emby-button', 'emby-checkbox'], function (loading) {
    'use strict';

    function loadPage(page, config) {

        page.querySelector('#txtUsername').value = config.Username || '';
        page.querySelector('#txtPassword').value = config.Password || '';

        loading.hide();
    }

    function onSubmit(e) {

        e.preventDefault();

        loading.show();

        var form = this;

        ApiClient.getNamedConfiguration("myanimelist").then(function (config) {

            config.Username = form.querySelector('#txtUsername').value;
            config.Password = form.querySelector('#txtPassword').value;

            ApiClient.updateNamedConfiguration("myanimelist", config).then(Dashboard.processServerConfigurationUpdateResult);
        });

        // Disable default form submission
        return false;
    }

    function getConfig() {

        return ApiClient.getNamedConfiguration("myanimelist");
    }

    return function (view, params) {

        view.querySelector('form').addEventListener('submit', onSubmit);

        view.addEventListener('viewshow', function () {

            loading.show();

            var page = this;

            getConfig().then(function (response) {

                loadPage(page, response);
            });
        });
    };

});
