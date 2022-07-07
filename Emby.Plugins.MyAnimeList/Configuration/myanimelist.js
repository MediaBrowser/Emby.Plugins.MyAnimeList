define(['loading', 'emby-input', 'emby-button', 'emby-checkbox'], function (loading) {
    'use strict';

    function loadPage(page, config) {

        page.querySelector('#txtOpenSubtitleUsername').value = config.OpenSubtitlesUsername || '';
        page.querySelector('#txtOpenSubtitlePassword').value = config.OpenSubtitlesPasswordHash || '';
        page.querySelector('#optionVip').checked = config.Vip;

        loading.hide();
    }

    function onSubmit(e) {

        e.preventDefault();

        loading.show();

        var form = this;

        ApiClient.getNamedConfiguration("opensubtitles").then(function (config) {

            config.OpenSubtitlesUsername = form.querySelector('#txtOpenSubtitleUsername').value;
            config.OpenSubtitlesPasswordHash = form.querySelector('#txtOpenSubtitlePassword').value;
            config.Vip = form.querySelector('#optionVip').checked;

            ApiClient.updateNamedConfiguration("opensubtitles", config).then(Dashboard.processServerConfigurationUpdateResult);
        });

        // Disable default form submission
        return false;
    }

    function getConfig() {

        return ApiClient.getNamedConfiguration("opensubtitles");
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
