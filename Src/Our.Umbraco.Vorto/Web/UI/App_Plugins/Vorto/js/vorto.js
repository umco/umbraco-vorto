angular.module("umbraco").controller("Our.Umbraco.PropertyEditors.Vorto.vortoEditor", [
    '$scope',
    '$rootScope',
    'appState',
    'editorState',
    'umbPropEditorHelper',
    'Our.Umbraco.Resources.Vorto.vortoResources',
    function ($scope, $rootScope, appState, editorState, umbPropEditorHelper, vortoResources) {

        var currentSection = appState.getSectionState("currentSection");

        $scope.languages = [];
        $scope.pinnedLanguages = [];
        $scope.$rootScope = $rootScope;

        $scope.currentLanguage = undefined;
        $scope.activeLanguage = undefined;

        var cookieUnsyncedProps = JSON.parse($.cookie('vortoUnsyncedProps') || "[]");
        $scope.sync = !_.contains(cookieUnsyncedProps, $scope.model.id);

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
    
            if (!dontBroadcast && $scope.sync) {

                // Update cookie
                $.cookie('vortoCurrentLanguage', language.isoCode);
                $.cookie('vortoActiveLanguage', language.isoCode);

                // Broadcast a resync
                $rootScope.$broadcast("reSync");

            } else {
                $scope.currentLanguage = $scope.activeLanguage = language;
            }

            // Close the menu (Not really the right way to do it :))
            $("#vorto-" + $scope.model.id)
                .find(".vorto-tabs__item--menu")
                .removeClass("active")
                .find(".vorto-menu").hide();
        };

        $scope.setActiveLanguage = function (language, dontBroadcast) {
            if (!dontBroadcast && $scope.sync) {

                // Update cookie
                $.cookie('vortoActiveLanguage', language.isoCode);

                // Broadcast a resync
                $rootScope.$broadcast("reSync");

            } else {
                $scope.activeLanguage = language;
            }
        };

        $scope.pinLanguage = function (language) {
            if ($scope.sync) {

                // Update cookie
                var cookiePinnedLanguages = JSON.parse($.cookie('vortoPinnedLanguages') || "[]");
                cookiePinnedLanguages.push(language.isoCode);
                cookiePinnedLanguages = _.uniq(cookiePinnedLanguages);
                $.cookie('vortoPinnedLanguages', JSON.stringify(cookiePinnedLanguages));

                // Broadcast a resync
                $rootScope.$broadcast("reSync");

            } else {
                $scope.pinnedLanguages.push(language);
            }
        };

        $scope.unpinLanguage = function (language) {
            if ($scope.sync) {

                // Update cookie
                var cookiePinnedLanguages = JSON.parse($.cookie('vortoPinnedLanguages') || "[]");
                cookiePinnedLanguages = _.reject(cookiePinnedLanguages, function (itm) {
                    return itm == language.isoCode;
                });
                $.cookie('vortoPinnedLanguages', JSON.stringify(cookiePinnedLanguages));

                // Broadcast a resync
                $rootScope.$broadcast("reSync");

            } else {
                $scope.pinnedLanguages = _.reject($scope.pinnedLanguages, function (itm) {
                    return itm.isoCode == language.isoCode;
                });
            }
        };

        $scope.isPinnable = function (language) {
            return $scope.currentLanguage.isoCode != language.isoCode && !_.find($scope.pinnedLanguages, function (itm) {
                return itm.isoCode == language.isoCode;
            });
        };

        $scope.$on("languageValueChange", function (evt, delta) {
            $scope.model.value.values = $.extend({},
                $scope.model.value.values,
                delta);
        });

        $scope.$on("reSync", function (evt) {
            reSync();
        });

        $scope.$watchCollection("pinnedLanguages", function (pinnedLanguages) {

            var activePinnedLanguage = _.find(pinnedLanguages, function (itm) {
                return itm.isoCode == $scope.activeLanguage.isoCode;
            });
            if (!activePinnedLanguage) {
                $scope.activeLanguage = $scope.currentLanguage;
            }

        });

        $scope.$watch("currentLanguage", function (language) {

            // If same as a pinned language, remove the pinned language
            var pinned = _.find($scope.pinnedLanguages, function (itm) {
                return itm.isoCode == language.isoCode;
            });
            if (pinned) {
                $scope.unpinLanguage(pinned);
            }

        });

        $scope.$watch("sync", function (shouldSync) {
            var tmp;
            if (shouldSync) {
                tmp = JSON.parse($.cookie('vortoUnsyncedProps') || "[]");
                tmp = _.reject(tmp, function (itm) {
                    return itm == $scope.model.id;
                });
                $.cookie('vortoUnsyncedProps', JSON.stringify(tmp));
                reSync();
            } else {
                tmp = JSON.parse($.cookie('vortoUnsyncedProps') || "[]");
                tmp.push($scope.model.id);
                tmp = _.uniq(tmp);
                $.cookie('vortoUnsyncedProps', JSON.stringify(tmp));
            }
        });

        $scope.$watch("model.value", function () {
            validateProperty();
        }, true);

        var reSync = function() {
            if ($scope.sync) {

                // Handle current language change
                var cookieCurrentLanguage = $.cookie('vortoCurrentLanguage');
                var currentLanguage = _.find($scope.languages, function(itm) {
                    return itm.isoCode == cookieCurrentLanguage;
                }) || $scope.currentLanguage;

                if (!$scope.currentLanguage || $scope.currentLanguage.isoCode != currentLanguage) {
                    $scope.setCurrentLanguage(currentLanguage, true);
                }

                // Handle active language change
                var cookieActiveLanguage = $.cookie('vortoActiveLanguage');
                var activeLanguage = _.find($scope.languages, function (itm) {
                    return itm.isoCode == cookieActiveLanguage;
                }) || $scope.activeLanguage;

                if (!$scope.activeLanguage || $scope.activeLanguage.isoCode != activeLanguage) {
                    $scope.setActiveLanguage(activeLanguage, true);
                }

                // Handle pinned language change
                var cookiePinnedLanguages = JSON.parse($.cookie('vortoPinnedLanguages') || "[]");
                var pinnedLanguages = _.filter($scope.languages, function (itm) {
                    return _.contains(cookiePinnedLanguages, itm.isoCode);
                });

                $scope.pinnedLanguages = pinnedLanguages;
                
            }
        }

        var validateProperty = function ()
        {
            // Validate value changes
            if ($scope.model.validation.mandatory) {

                var mandatoryBehaviour = $scope.model.config.mandatoryBehaviour;
                var primaryLanguage = $scope.model.config.primaryLanguage;

                if (mandatoryBehaviour == "primary" && primaryLanguage == undefined) {
                    mandatoryBehaviour = "ignore";
                }

                //TODO: Might be better if we could get the inner control to validate this?

                var isValid = true;
                switch (mandatoryBehaviour) {
                    case "all":
                        _.each($scope.languages, function (language) {
                            if (!(language.isoCode in $scope.model.value.values) ||
                                !$scope.model.value.values[language.isoCode]) {
                                isValid = false;
                                return;
                            }
                        });
                        break;
                    case "any":
                        isValid = false;
                        _.each($scope.languages, function (language) {
                            if (language.isoCode in $scope.model.value.values &&
                                $scope.model.value.values[language.isoCode]) {
                                isValid = true;
                                return;
                            }
                        });
                        break;
                    case "primary":
                        if (primaryLanguage in $scope.model.value.values
                            && $scope.model.value.values[primaryLanguage]) {
                            isValid = true;
                        } else {
                            isValid = false;
                        }
                        break;
                }

                $scope.vortoForm.$setValidity("required", isValid);

                // TODO: Regex
            }
        }

        // Load the datatype
        vortoResources.getDataTypeById($scope.model.config.dataType.guid).then(function (dataType) {

            // Stash the config in scope for reuse
            $scope.property.config = dataType.preValues;

            // Get the view path
            $scope.property.viewPath = umbPropEditorHelper.getViewPath(dataType.view);

            // Get the current properties datatype
            vortoResources.getDataTypeByAlias(currentSection, editorState.current.contentTypeAlias, $scope.model.alias).then(function (dataType2) {

                $scope.model.value.dtdguid = dataType2.guid;

                // Load the languages (this will trigger everything else to bind)
                vortoResources.getLanguages(currentSection, editorState.current.id, editorState.current.parentId, dataType2.guid)
                    .then(function (languages) {

                        $scope.languages = languages;
                        $scope.currentLanguage = $scope.activeLanguage = _.find(languages, function (itm) {
                            return itm.isDefault;
                        });

                        reSync();

                        validateProperty();
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

angular.module("umbraco").controller("Our.Umbraco.PreValueEditors.Vorto.languagePicker", [
    '$scope',
    'Our.Umbraco.Resources.Vorto.vortoResources',
    function ($scope, vortoResources) {

        $scope.model.languages = [];

        vortoResources.getInstalledLanguages().then(function (data) {
            $scope.model.languages = data;
        });

    }]
);

/* Directives */
angular.module("umbraco.directives").directive('vortoProperty',
    function ($compile, $http, umbPropEditorHelper, $timeout, $rootScope, $q) {

        var link = function (scope, element, attrs, ctrl) {
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
            getDataTypeByAlias: function (contentType, contentTypeAlias, propertyAlias) {
                return umbRequestHelper.resourcePromise(
                    $http.get("/umbraco/backoffice/VortoApi/VortoApi/GetDataTypeByAlias?contentType=" + contentType + "&contentTypeAlias=" + contentTypeAlias + "&propertyAlias=" + propertyAlias),
                    'Failed to retrieve datatype'
                );
            },
            getLanguages: function (section, id, parentId, dtdguid) {
                return umbRequestHelper.resourcePromise(
                    $http.get("/umbraco/backoffice/VortoApi/VortoApi/GetLanguages?section=" + section + "&id=" + id + "&parentId=" + parentId + "&dtdguid=" + dtdguid),
                    'Failed to retrieve languages'
                );
            },
            getInstalledLanguages: function () {
                return umbRequestHelper.resourcePromise(
                    $http.get("/umbraco/backoffice/VortoApi/VortoApi/GetInstalledLanguages"),
                    'Failed to retrieve languages'
                );
            }
        };
    }
);

$(function () {

    var over = function () {
        var self = this;
        $(self).addClass("active").find(".vorto-menu").show().css('z-index', 9000 );
    };

    var out = function () {
        var self = this;
        $(self).removeClass("active").find(".vorto-menu").hide().css('z-index', 0 );
    };

    $("body").hoverIntent({
        over: over,
        out: out,
        interval: 10,
        timeout: 250,
        selector: ".vorto-tabs__item--menu"
    });

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