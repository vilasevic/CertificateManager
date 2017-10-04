var CmOptions = {
    hashAlgorithmOptions: [
        { Name: "SHA1", Id: 0, Display: "SHA1 (Insecure)" },
        { Name: "SHA256", Id: 1, Display: "SHA256 (Recommended)" },
        { Name: "SHA512", Id: 2, Display: "SHA512 (Most Secure)", Primary: true }
    ],
    cipherOptions: [
        { Name: "RSA", Id: 0, Display: "RSA (TLS / More Support)", Primary: true },
        { Name: "ECDH", Id: 1, Display: "ECDH (TLS / Most Secure)" },
        { Name: "ECDSA", Id: 2, Display: "ECDSA (Uncommon)" }
    ],

    keyUsageOptions: [
        { Name: "None", Id: 0, Primitive: true, Display: "None" },
        { Name: "ServerAuthentication", Id: 1, Primitive: true, Display: "ServerAuthentication", Primary: true },
        { Name: "ClientAuthentication", Id: 2, Primitive: true, Display: "ClientAuthentication" },
        { Name: "ServerAuthentication, ClientAuthentication", Id: 3, Primitive: false, Display: "ServerAuthentication, ClientAuthentication" },
        { Name: "DocumentEncryption", Id: 4, Primitive: true, Display: "DocumentEncryption" },
        { Name: "CodeSigning", Id: 8, Primitive: true, Display: "CodeSigning" },
        { Name: "CertificateAuthority", Id: 16, Primitive: true, Display: "CertificateAuthority" },
        { Name: "Undetermined", Id: 32, Primitive: true, Display: "Undetermined" }

    ],
    windowsApiOptions: [
        { Name: "Cng", Id: 1, Display: "CryptoApi Next Generation (Most Secure)", Primary: true },
        { Name: "CryptoApi", Id: 0, Display: "CryptoApi (More Support)" }
    ],
    //windowsApiOptions: ["Cng", "CryptoApi"],
    authenticationTypeOptions: [
        { Name: "UsernamePassword", Id: 0, Display: "basic" },
        { Name: "WindowsKerberos", Id: 1, Display: "kerberos" }
    ],
    ExternalIdentitySourceType: [
        { Name: "ActiveDirectoryIwa", Id: 0, Display: "ActiveDirectoryIwa" },
        { Name: "ActiveDirectoryBasic", Id: 1, Display: "ActiveDirectoryBasic" },
    ]
}

var baseUri = "http://certificatemanager/"
var certSearchResult = null;


var Services = {

    CreateCertificate: function (request, successCallback, errorCallback) {
        $.ajax({
            url: "/ca/private/certificate/request/includeprivatekey",
            type: 'post',
            data: {
                SubjectCommonName: request.SubjectCommonName,
                SubjectDepartment: request.SubjectDepartment,
                SubjectOrganization: request.SubjectOrganization,
                SubjectCity: request.SubjectCity,
                SubjectState: request.SubjectState,
                SubjectCountry: request.SubjectCountry,
                SubjectAlternativeNamesRaw: request.SubjectAlternativeNamesRaw,
                CipherAlgorithm: request.CipherAlgorithm,
                Provider: request.Provider,
                HashAlgorithm: request.HashAlgorithm,
                KeySize: request.KeySize,
                KeyUsage: request.KeyUsage
            },
            cache: false,
            async: true,
            dataType: "json",
            success: function (data) {
                successCallback(data);
            },
            error: function (x, t, m) {
                errorCallback(x, t, m);
            }
        });
    },

    GetCertificateDetails: function (id, successCallback, errorCallback) {
        $.ajax({
            url: "/certificate/" + id,
            type: 'get',
            cache: false,
            async: true,
            dataType: "json",
            success: function (data) {
                successCallback(data);
            },
            error: function (x, t, m) {
                errorCallback(x, t, m);
            }
        });
    },

    SearchCertificates: function (query, successCallback, errorCallback)
    {
        $.ajax({
            url: "/certificates/search",
            type: 'get',
            cache: false,
            async: true,
            dataType: "json",
            success: function (data) {
                certSearchResult = data;
                //successCallback(data);
            },
            error: function (x, t, m) {
                //errorCallback(x, t, m);
            }
        });
    },

    GetAdcsTemplates: function (successCallback, errorCallback) {
        $.ajax({
            url: "/pki-config/templates",
            type: 'get',
            cache: false,
            async: true,
            dataType: "json",
            success: function (data) {
                successCallback(data);
            },
            error: function (x, t, m) {
            }
        });
    },

    GetEnumMapping: async function () {
        const response = await axios.get("/view/enum-mapping");
        localStorage.setItem("uiEnumMap", JSON.stringify(response.data));

    },

    GetSecurityRoleDetails: function (id, successCallback, errorCallback)
    {
        $.ajax({
            url: "/security/role/" + id,
            type: 'get',
            cache: false,
            async: true,
            dataType: "json",
            success: function (data) {
                successCallback(data);
            },
            error: function (x, t, m) {
                errorCallback(x, t, m);
            }
        });
    },

    ImportUsersFromExternalIdentitySource: function (data, successCallback, errorCallback)
    {
        $.ajax({
            url: "/security/authenticable-principal/import",
            type: 'post',
            cache: false,
            async: true,
            dataType: "json",
            data: data,
            success: function (data) {
                successCallback(data);
            },
            error: function (x, t, m) {
                errorCallback(x, t, m);
            }
        });
    },

    GetAppConfig: function (successCallback, errorCallback)
    {
        $.ajax({
            url: "/config",
            type: 'get',
            cache: false,
            async: true,
            dataType: "json",
            success: function (data) {
                successCallback(data);
            },
            error: function (x, t, m) {
                errorCallback(x, t, m);
            }
        });
    },

    SetAppConfig: function (data, successCallback, errorCallback)
    {
        $.ajax({
            url: "/config",
            type: 'put',
            cache: false,
            async: true,
            data: data,
            dataType: "json",
            success: function (data) {
                successCallback();
            },
            error: function (x, t, m) {
                errorCallback(x, t, m);
            }
        });
    },

    ResetUserPassword: function (data, successCallback, errorCallback) {
        $.ajax({
            url: "/security/authenticable-principal/password",
            type: 'put',
            cache: false,
            async: true,
            data: data,
            dataType: "json",
            success: function (response) {
                successCallback("Successfully reset password");
            },
            error: function (x, t, m) {
                errorCallback(x, t, m);
            }
        });
    },

}

function openTab(evt, tabName) {
    // Declare all variables
    var i, tabcontent, tablinks;

    location.hash = tabName;
    // Get all elements with class="tabcontent" and hide them
    tabcontent = document.getElementsByClassName("tabcontent");
    for (i = 0; i < tabcontent.length; i++) {
        tabcontent[i].style.display = "none";
    }

    // Get all elements with class="tablinks" and remove the class "active"
    tablinks = document.getElementsByClassName("tablinks");
    for (i = 0; i < tablinks.length; i++) {
        tablinks[i].className = tablinks[i].className.replace(" active", "");
    }

    // Show the current tab, and add an "active" class to the button that opened the tab
    document.getElementById(tabName).style.display = "block";
    $('.' + tabName).addClass("active");
};


var UIDefaults = {
    GetEnumMap: function () {
        if (localStorage.getItem("uiEnumMap") === null) {
            Services.GetEnumMapping();

            while (localStorage.getItem("uiEnumMap") === null)
            {

            }
            return JSON.parse(localStorage.getItem("uiEnumMap"));
        }
        else {
            return JSON.parse(localStorage.getItem("uiEnumMap"));
        }
    }
};


var UiGlobal = {
    RefreshGrid: function (grid)
    {
        grid.jsGrid("render");
    },
    ResetAlertState: function ()
    {
        UiGlobal.HideError();
        UiGlobal.HideSuccess();
    },
    ShowSuccess: function (msg)
    {
        $('#success-alert').text(msg)
        $('#success-alert').show();
    },
    HideSuccess: function (msg)
    {
        $('#success-alert').hide();
    },
    ShowError: function (msg)
    {
        $('#error-alert').text(msg)
        $('#error-alert').show();
    },
    HideError: function ()
    {
        $('#error-alert').hide();
    },
    ShowCurrentTab: function ()
    {
        if (location.hash === "" || location.hash === null) {
            $('#defaultOpen').click();
        }
        else
        {
            var currentTab = location.hash.replace("#", "");
            $("." + currentTab).click();
        }
    },

    ShowModal: function (id) {
        $("#" + id).modal("show");
    },
    GetSelectedOptions: function (obj)
    {
        var selectedArray = [];

        var selected = obj.find(":selected");

        for (i = 0; i < selected.length; i++) {
            selectedArray.push(selected[i].value);
        }

        return selectedArray;
    },

    GetDateString(arg) {
        return (new Date(arg)).toDateString();
    }
}