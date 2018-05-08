
$(document).ready(function () {

    // get list of records via ajax:  NBrightRazorTemplate_nbxget({command}, {div of data passed to server}, {return html to this div} )
    nbxget('os_bulkedit_getdata', '#selectparams', '#editdata');

    $('.actionbuttonwrapper #cmdsave').unbind('click');
    $('.actionbuttonwrapper #cmdsave').click(function () {
        nbxget('os_bulkedit_savedata', '#editdata');
    });

    $('.actionbuttonwrapper #cmdreturn').unbind('click');
    $('.actionbuttonwrapper #cmdreturn').click(function () {
        $('#selecteditemid').val(''); // clear sleecteditemid.        
        nbxget('os_bulkedit_getdata', '#selectparams', '#editdata');
    });

    $('.actionbuttonwrapper #cmddelete').unbind('click');
    $('.actionbuttonwrapper #cmddelete').click(function () {
        if (confirm($('#deletemsg').val())) {
            nbxget('os_bulkedit_deleterecord', '#editdata');
        }
    });

    $('#addnew').unbind('click');
    $('#addnew').click(function () {
        $('.processing').show();
        $('#newitem').val('new');
        $('#selecteditemid').val('');
        nbxget('os_bulkedit_addnew', '#selectparams', '#editdata');
    });

    $('.selecteditlanguage').unbind('click');
    $('.selecteditlanguage').click(function () {
        $('#nextlang').val($(this).attr('lang')); // alter lang after, so we get correct data record
        nbxget('os_bulkedit_selectlang', '#editdata'); // do ajax call to save current edit form
    });


});

$(document).on("nbxgetcompleted", nbxgetCompleted); // assign a completed event for the ajax calls


function nbxgetCompleted(e) {

    if (e.cmd == 'os_bulkedit_addnew') {
        $('#newitem').val(''); // clear item so if new was just created we don;t create another record
        $('#selecteditemid').val($('#itemid').val()); // move the itemid into the selecteditemid, so page knows what itemid is being edited
        OS_BulkEdit_DetailButtons();
        $('.processing').hide(); 
    }

    if (e.cmd == 'os_bulkedit_deleterecord') {
        $('#selecteditemid').val(''); // clear selecteditemid, it now doesn;t exists.
        nbxget('os_bulkedit_getdata', '#selectparams', '#editdata');// relist after delete
    }

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
    }

    // check if we are displaying a list or the detail and do processing.
    if (($('#selecteditemid').val() != '') || (e.cmd == 'os_bulkedit_addnew')) {
        // PROCESS DETAIL
        OS_BulkEdit_DetailButtons();

        if ($('input:radio[name=typeselectradio]:checked').val() == "cat") {
            $('.catdisplay').show();
            $('.propdisplay').hide();
        } else {
            $('.catdisplay').hide();
            $('.propdisplay').show();
        }

        $('input:radio[name=typeselectradio]').unbind('change');
        $('input:radio[name=typeselectradio]').change(function () {
            if ($(this).val() == 'cat') {
                $('.catdisplay').show();
                $('.propdisplay').hide();
            } else {
                $('.catdisplay').hide();
                $('.propdisplay').show();
            }
        });

        if ($('input:radio[name=applydiscounttoradio]:checked').val() == "1") {
            $('.applyproperty').hide();
        } else {
            $('.applyproperty').show();
        }

        $('input:radio[name=applydiscounttoradio]').unbind('change');
        $('input:radio[name=applydiscounttoradio]').change(function () {
            if ($(this).val() == '1') {
                $('.applyproperty').hide();
            } else {
                $('.applyproperty').show();
            }
        });

        if ($('.applydaterangechk').is(":checked")) {
            $('.applydaterange').show();
        }

        $('.applydaterangechk').unbind('change');
        $('.applydaterangechk').change(function () {
            if ($(this).is(":checked")) {
                $('.applydaterange').show();
            } else {
                $('.applydaterange').hide();
            }
        });

        $('#cmdrecalcpromo').unbind('click');
        $('#cmdrecalcpromo').click(function () {
            if (confirm($('#deletemsg').val())) {
                $('.processing').show();
                nbxget('os_bulkedit_recalc', '#selectparams', '#editdata'); // do ajax call to get edit form
            }
        });

        $('.processing').hide(); 

    } else {
        //PROCESS LIST
        OS_BulkEdit_ListButtons();
        $('.edititem').unbind('click');
        $('.edititem').click(function () {
            $('.processing').show();
            $('#selecteditemid').val($(this).attr("itemid")); // assign the sleected itemid, so the server knows what item is being edited
            nbxget('os_bulkedit_getdata', '#selectparams', '#editdata'); // do ajax call to get edit form
        });
        $(".catdisplay").prop("disabled", true);
        $(".propdisplay").prop("disabled", true);

        $('.processing').hide(); 
    }



}

function OS_BulkEdit_DetailButtons() {
    $('#cmdsave').show();
    $('#cmddelete').show();
    $('#cmdreturn').show();
    $('#addnew').hide();
    $('input[datatype="date"]').datepicker(); // assign datepicker to any ajax loaded fields
}

function OS_BulkEdit_ListButtons() {
    $('#cmdsave').hide();
    $('#cmddelete').hide();
    $('#cmdreturn').hide();
    $('#addnew').show();
}


