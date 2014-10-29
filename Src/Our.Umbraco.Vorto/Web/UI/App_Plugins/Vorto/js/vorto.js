angular.module("umbraco").controller("Our.Umbraco.PropertyEditors.Vorto.vortoEditor", [
    '$scope',
    '$rootScope',
    'editorState',
    'umbPropEditorHelper',
    'Our.Umbraco.Resources.Vorto.vortoResources',
    function ($scope, $rootScope, editorState, umbPropEditorHelper, vortoResources) {

        $scope.languages = [];
        $scope.pinnedLanguages = [];
        $scope.$rootScope = $rootScope;

        $scope.currentLanguage = undefined;
        $scope.activeLanguage = undefined;

        $scope.model.hideLabel = $scope.model.config.hideLabel == 1;

        $scope.property = {
            config: {},
            view: ""
        };

        $scope.model.value = {
            values: $.extend({}, $scope.model.value.values),
            dtdguid: 0
        };

        $scope.setCurrentLanguage = function (language, dontBroadcast) {
            // If same as a pinned language, remove the pinned language
            var pinned = _.find($scope.pinnedLanguages, function(itm) {
                return itm.isoCode == language.isoCode;
            });
            if (pinned) {
                $scope.unpinLanguage(pinned);
            }

            // Set current language
            $scope.currentLanguage = $scope.activeLanguage = language;

            // Broadcast
            if (!dontBroadcast && $rootScope.vortoSyncAll) {
                $rootScope.$broadcast("syncCurrentLanguage", language);
            };

            // Close the menu (Not really the right way to do it :))
            $("#vorto-" + $scope.model.id)
                .find(".vorto-tabs__item--menu")
                .removeClass("active")
                .find(".vorto-menu").hide();
        };

        $scope.setActiveLanguage = function (language) {
            $scope.activeLanguage = language;
        };

        $scope.pinLanguage = function (language) {
            $scope.pinnedLanguages.push(language);
        };

        $scope.unpinLanguage = function (language) {
            if ($scope.activeLanguage.isoCode == language.isoCode) {
                $scope.activeLanguage = $scope.currentLanguage;
            }
            $scope.pinnedLanguages = _.reject($scope.pinnedLanguages, function(itm) {
                return itm.isoCode == language.isoCode;
            });
        };

        $scope.isPinnable = function (language)
        {
            return $scope.currentLanguage.isoCode != language.isoCode && !_.find($scope.pinnedLanguages, function(itm) {
                return itm.isoCode == language.isoCode;
            });
        };

        $scope.$on("languageValueChange", function (evt, delta) {
            $scope.model.value.values = $.extend({},
                $scope.model.value.values,
                delta);
        });

        $scope.$on("syncCurrentLanguage", function (evt, language) {
            $scope.setCurrentLanguage(language, true);
        });

        // Load the datatype
        vortoResources.getDataTypeById($scope.model.config.dataType.guid).then(function (dataType) {

            // Create the property config
            var configObj = {};
            _.each(dataType.preValues, function (p) {
                configObj[p.key] = p.value;
            });

            // Stash the config in scope for reuse
            $scope.property.config = configObj;

            // Get the view path
            $scope.property.viewPath = umbPropEditorHelper.getViewPath(dataType.view);

            // Get the current properties datatype
            vortoResources.getDataTypeByAlias(editorState.current.contentTypeAlias, $scope.model.alias).then(function (dataType2) {

                $scope.model.value.dtdguid = dataType2.guid;

                // Load the languages (this will trigger everything else to bind)
                vortoResources.getLanguages(editorState.current.id, editorState.current.parentId, dataType2.guid)
                    .then(function (languages) {
                        $scope.languages = languages;
                        $scope.currentLanguage = $scope.activeLanguage = _.find(languages, function(itm) {
                            return itm.isDefault;
                        });
                    });
            });
        });

        
    }
]);

angular.module("umbraco").controller("Our.Umbraco.PreValueEditors.Vorto.propertyEditorPicker", [
    '$scope',
    'Our.Umbraco.Resources.Vorto.vortoResources',
    function ($scope, vortoResources) {

        $scope.model.dataTypes = [];
        $scope.model.value = $scope.model.value || {
            guid: "0cc0eba1-9960-42c9-bf9b-60e150b429ae",
            name: "Textstring",
            propertyEditorAlias: "Umbraco.Textbox"
        };

        vortoResources.getNonVortoDataTypes().then(function (data) {
            $scope.model.dataTypes = data;
        });

    }]
);

/* Directives */
angular.module("umbraco.directives").directive('vortoProperty',
    function ($compile, $http, umbPropEditorHelper, $timeout, $rootScope, $q) {

        var link = function (scope, element, attrs, ctrl)
        {
            scope[ctrl.$name] = ctrl;

            scope.model = {};
            scope.model.config = scope.config;
            scope.model.alias = "vorto-" + scope.language + "-" + scope.propertyAlias;
            scope.model.value = scope.value;

            scope.$watch('model.value', function (newValue, oldValue) {
                var obj = {};
                obj[scope.language] = newValue;
                scope.$emit('languageValueChange', obj);
            }, true);
        };

        return {
            require: "^form",
            restrict: "E",
            rep1ace: true,
            link: link,
            templateUrl: 'views/directives/umb-editor.html',
            scope: {
                propertyEditorView: '=view',
                config: '=',
                language: '=',
                propertyAlias: '=',
                value: '='
            }
        };
    });

angular.module('umbraco.directives').directive('jsonText', function () {
    return {
        restrict: 'A',
        require: 'ngModel',
        link: function (scope, element, attr, ngModel) {
            function into(input) {
                return JSON.parse(input);
            }
            function out(data) {
                return JSON.stringify(data);
            }
            ngModel.$parsers.push(into);
            ngModel.$formatters.push(out);

        }
    };
});

/* Resources */
angular.module('umbraco.resources').factory('Our.Umbraco.Resources.Vorto.vortoResources',
    function ($q, $http, umbRequestHelper) {
        return {
            getNonVortoDataTypes: function () {
                return umbRequestHelper.resourcePromise(
                    $http.get("/umbraco/backoffice/VortoApi/VortoApi/GetNonVortoDataTypes"),
                    'Failed to retrieve datatypes'
                );
            },
            getDataTypeById: function (id) {
                return umbRequestHelper.resourcePromise(
                    $http.get("/umbraco/backoffice/VortoApi/VortoApi/GetDataTypeById?id=" + id),
                    'Failed to retrieve datatype'
                );
            },
            getDataTypeByAlias: function (contentTypeAlias, propertyAlias) {
                return umbRequestHelper.resourcePromise(
                    $http.get("/umbraco/backoffice/VortoApi/VortoApi/GetDataTypeByAlias?contentTypeAlias=" + contentTypeAlias + "&propertyAlias=" + propertyAlias),
                    'Failed to retrieve datatype'
                );
            },
            getLanguages: function (id, parentId, dtdguid) {
                return umbRequestHelper.resourcePromise(
                    $http.get("/umbraco/backoffice/VortoApi/VortoApi/GetLanguages?id=" + id + "&parentId=" + parentId + "&dtdguid=" + dtdguid),
                    'Failed to retrieve languages'
                );
            }
        };
    }
);

$(function() {

    var over = function() {
        var self = this;
        $(self).addClass("active").find(".vorto-menu").show();
    };

    var out = function() {
        var self = this;
        $(self).removeClass("active").find(".vorto-menu").hide();
    };

    $("body").hoverIntent({
        over: over,
        out: out,
        interval: 10,
        timeout: 250,
        selector: ".vorto-tabs__item--menu"
    });

});