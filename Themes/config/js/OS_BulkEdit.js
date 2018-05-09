
$(document).ready(function () {

    nbxget('os_bulkedit_getdata', '#selectparams', '#editdata');
    $('.processing').hide(); 

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

    if (e.cmd == 'os_bulkedit_selectchangedisable' || e.cmd == 'os_bulkedit_selectchangehidden') {
        $('.processing').hide();
    };

    if (e.cmd == 'os_bulkedit_deleterecord' || e.cmd == 'os_bulkedit_saveitem') {
        nbxget('os_bulkedit_getdata', '#selectparams', '#editdata');
    };    

    if (e.cmd == 'os_bulkedit_getdata') {
        $('.processing').hide();
        $('.selecteditlanguage').show();

        $("#ddllistsearchcategory").unbind("change");
        $("#ddllistsearchcategory").change(function () {
            $('#searchcategory').val($("#ddllistsearchcategory").val());
            nbxget('os_bulkedit_getdata', '#selectparams', '#editdata');
        });

        $("#chkcascaderesults").unbind("change");
        $("#chkcascaderesults").change(function () {
            if ($("#chkcascaderesults").is(':checked')) {
                $('#cascade').val("True");
            } else {
                $('#cascade').val("False");
            }
            nbxget('os_bulkedit_getdata', '#selectparams', '#editdata');
        });

        $('.selectchangedisable').unbind("click");
        $('.selectchangedisable').click(function () {
            $('.processing').show();
            $('#selecteditemid').val($(this).attr('itemid'));
            if ($(this).hasClass("fa-check-circle")) {
                $(this).addClass('fa-circle').removeClass('fa-check-circle');
            } else {
                $(this).addClass('fa-check-circle').removeClass('fa-circle');
            }
            nbxget('os_bulkedit_selectchangedisable', '#selectparams');
        });

        $('.selectchangehidden').unbind("click");
        $('.selectchangehidden').click(function () {
            $('.processing').show();
            $('#selecteditemid').val($(this).attr('itemid'));
            if ($(this).hasClass("fa-check-circle")) {
                $(this).addClass('fa-circle').removeClass('fa-check-circle');
            } else {
                $(this).addClass('fa-check-circle').removeClass('fa-circle');
            }
            nbxget('os_bulkedit_selectchangehidden', '#selectparams');
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

        $('.productAdmin_cmdDelete').unbind("click");
        $('.productAdmin_cmdDelete').click(function () {
            if (confirm($('#confirmdeletemsg').text())) {
                $('.actionbuttonwrapper').hide();
                $('.editlanguage').hide();
                $('.processing').show();
                $('#selecteditemid').val($(this).attr('itemid'));
                nbxget('os_bulkedit_deleterecord', '#selectparams', '#editdata');
            }
        });

        $('#cmdsave').unbind("click");
        $('#cmdsave').click(function () {
            $('.processing').show();
            $('#selecteditemid').val($(this).attr('itemid'));
            $('.productAdmin_cmdSaveItem').hide();

            //build XML for all records.
            var xdat = '<root>';
            $('.modelrow').each(function (i, obj) {
                if ($('#isdirty_' + $(this).attr('itemid')).val() == '1') {
                    xdat += "<models productid='" + $(this).attr('itemid') + "'>";
                    xdat += $.fn.genxmlajaxitems($(this), '.modelitem');
                    xdat += "</models>";
                }
            });
            xdat += "</root>";

            //move data to update postback field
            $('#xmlupdatemodeldata').val(xdat);

            $('#cmdsave').hide();
            nbxget('os_bulkedit_saveitem', '#selectparams');
        });        


        $('.modelfield').unbind("keyup");
        $('.modelfield').keyup(function () {
            $('#cmdsave').show();
            $('.selecteditlanguage').hide();
            $('#isdirty_' + $(this).attr('itemid')).val('1');            
        });

    }

}

