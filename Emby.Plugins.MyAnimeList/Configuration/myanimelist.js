define(['baseView', 'loading', 'emby-input', 'emby-button', 'emby-checkbox', 'emby-scroller'], function (BaseView, loading) {
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

    function View(view, params) {
        BaseView.apply(this, arguments);

        view.querySelector('form').addEventListener('submit', onSubmit);
    }

    Object.assign(View.prototype, BaseView.prototype);

    View.prototype.onResume = function (options) {

        BaseView.prototype.onResume.apply(this, arguments);

        loading.show();

        var page = this.view;

        getConfig().then(function (response) {

            loadPage(page, response);
        });
    };

    return View;

});
