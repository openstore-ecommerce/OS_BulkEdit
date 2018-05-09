
$(document).ready(function () {

    // get list of records via ajax:  NBrightRazorTemplate_nbxget({command}, {div of data passed to server}, {return html to this div} )
    nbxget('os_bulkedit_getdata', '#selectparams', '#editdata');

    $('.actionbuttonwrapper #cmdsave').unbind('click');
    $('.actionbuttonwrapper #cmdsave').click(function () {
        nbxget('os_bulkedit_savedata', '#editdata');
    });

    $('.selecteditlanguage').unbind('click');
    $('.selecteditlanguage').click(function () {
        $('#nextlang').val($(this).attr('lang')); // alter lang after, so we get correct data record
        nbxget('os_bulkedit_selectlang', '#editdata'); // do ajax call to save current edit form
    });

});

$(document).on("nbxgetcompleted", nbxgetCompleted); // assign a completed event for the ajax calls


function nbxgetCompleted(e) {

    if (e.cmd == 'os_bulkedit_savedata') {
        $('#selecteditemid').val(''); // clear sleecteditemid.        
        nbxget('os_bulkedit_getdata', '#selectparams', '#editdata');// relist after save
    }

    if (e.cmd == 'os_bulkedit_selectlang') {
        $('#editlang').val($('#nextlang').val()); // alter lang after, so we get correct data record
        nbxget('os_bulkedit_getdata', '#selectparams', '#editdata'); // do ajax call to get edit form
    }

    if (e.cmd == 'os_bulkedit_getdata') {
        $('.processing').hide();

        $("#ddllistsearchcategory").unbind("change");
        $("#ddllistsearchcategory").change(function () {
            $('#searchcategory').val($("#ddllistsearchcategory").val());
            nbxget('os_bulkedit_getdata', '#selectparams', '#editdata');
        });


        $('.cmdPg').unbind("click");
        $('.cmdPg').click(function () {
            $('.processing').show();
            $('#pagenumber').val($(this).attr('pagenumber'));
            nbxget('os_bulkedit_getdata', '#selectparams', '#editdata');
        });

        $('#productAdmin_cmdSearch').unbind("click");
        $('#productAdmin_cmdSearch').click(function () {
            $('.processing').show();
            $('#pagenumber').val('1');
            $('#searchtext').val($('#productAdmin_searchtext').val());
            nbxget('os_bulkedit_getdata', '#selectparams', '#editdata');
        });

        $('#productAdmin_cmdReset').unbind("click");
        $('#productAdmin_cmdReset').click(function () {
            $('.processing').show();
            $('#pagenumber').val('1');
            $('#searchtext').val('');
            $("#searchcategory").val('');
            $("#ddllistsearchcategory").get(0).selectedIndex = 0;;
            
            nbxget('os_bulkedit_getdata', '#selectparams', '#editdata');
        });


    }

    // check if we are displaying a list or the detail and do processing.
        //PROCESS LIST
        OS_BulkEdit_ListButtons();
        $(".catdisplay").prop("disabled", true);
        $(".propdisplay").prop("disabled", true);
    $('#cmdsave').show();

        $('.processing').hide(); 


}

